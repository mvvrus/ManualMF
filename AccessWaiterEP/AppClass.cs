using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Configuration;
using System.Web.Script.Serialization;
using AccessWaiterEP.Infrastructure;
using ManualMF;

namespace AccessWaiterEP
{
    class AppClass: IAppInstance,IDisposable
    {
        const int RECONNECT_TIMEOUT = 1800000;

        const int CANCEL_COMMAND = -2;
        const int ABANDON_COMMAND = -1;

        class OutstandingTaskInfo
        {
            public Task Task;
            public CancellationTokenSource CTSource;
        }

        int m_Key;
        volatile IAppInstanceFactory m_Factory=null;
        volatile Task<Boolean> m_WaitTask = null;
        volatile CancellationTokenSource m_WaitCts = null;
        Object m_WaitLock = new Object();
        volatile OutstandingTaskInfo m_Outstanding = null;
        volatile int m_OutstandingTaskCount = 0;
        volatile Boolean m_Disposed=false;

        class WaitTaskParams
        {
            public String Upn=null;
            public int AccessToken=0;
        }

        SqlConnection m_Conn = null; 
        AccessWaiter m_AccessWaiter;

        internal AppClass(IAppInstanceFactory Factory, int Key)
        {
            m_Factory = Factory;
            m_Key = Key;
            //Create and open database connection
            m_Conn = new SqlConnection((string)Factory.GetConfigurationItem(Global.CONN_STR));
            m_Conn.Open();
            //Create AccessWaiter object
            m_AccessWaiter = new AccessWaiter(m_Conn
                , (int)Factory.GetConfigurationItem(Global.MAXWAITTIME)
                , (int)Factory.GetConfigurationItem(Global.FIRSTWAITTIME)
            );
        }

        Task<bool> StartAsyncMethod(WaitTaskParams Params, CancellationToken Ct)
        {
            return m_AccessWaiter.WaitForAccessChangeAsync(Params.Upn, Params.AccessToken, Ct);
        }

        //Clean up wait task and all appropriate stuff
        private void CleanupWaitTask()
        {
            lock (m_WaitLock)
            {
                CancellationTokenSource ct = m_WaitCts;
                Task t = m_WaitTask;
                m_WaitTask = null;
                if (t != null && t.IsCompleted) t.Dispose();
                m_WaitCts = null;
                if (ct != null) ct.Dispose();
                #pragma warning disable 0420 //Supress excessive warnings about volatile field used in Interlocked method
                OutstandingTaskInfo outstanding = Interlocked.Exchange<OutstandingTaskInfo>(ref m_Outstanding, null);
                #pragma warning restore 0420 //Restore supressed warning
                if (outstanding!=null && outstanding.Task != null && !outstanding.Task.IsCompleted) outstanding.CTSource.Cancel();
                m_OutstandingTaskCount = 0;
            }
        }

        //Function performed as a resulting task
        internal string EndWaitForChangeFunction(Task<bool> AntecendentTask,Object State)
        {
            //Fast return if this task was abandoned (canceled)
            OutstandingTaskInfo outstanding = State as OutstandingTaskInfo;
            if (outstanding != null) outstanding.CTSource.Token.ThrowIfCancellationRequested();
            else throw new ArgumentException();
            Abandon(outstanding.Task,false);
            return MakeResult(AntecendentTask);
        }

        private static string MakeResult(Task<bool> AntecendentTask)
        {
            Boolean antecendent_result = AntecendentTask.GetAwaiter().GetResult(); //Throws exception, if any occured in the antecendent task
            if (!antecendent_result) throw new System.Web.HttpException(403, "Forbidden"); //Access to the wait was forbidden
            return "{}"; //Normal return - no data to return
        }

        //Action to be performed after completion of an outstanding task: a resulting task or a guard task (task, that keeps the wait task to stay in place until its completion)
        //The action is to decrement (in a thread-safe manner) outstanding task count and cleaning up the wait task then it reaches zero
        internal void CheckForWaitTaskCleanupAction(Task NotUsed)
        {
            #pragma warning disable 0420 //Supress excessive warnings about volatile field used in Interlocked method
            if (Interlocked.Decrement(ref m_OutstandingTaskCount) <= 0) CleanupWaitTask();
            #pragma warning restore 0420 //Restore supressed warning
        }

        //Action for the task to be performed after the timeout for the reconnect (new WaitForAccessChange call after previous Abandon call)
        //The action is to cancel wait task and remove this object from the list of application instaces
        internal void TimeoutAction(Task NotUsed, Object Cts)
        {
            //Cancel wait task
            lock (m_WaitLock) //Should protect from race because this task is not counted as an outstanding task
            {
                if (!m_Disposed && m_WaitCts!=null && m_WaitCts == Cts) m_WaitCts.Cancel();
            }
            //remove this object from the list of application instaces (effectively disposing the object) in a thread-safe manner
            #pragma warning disable 0420 //Supress excessive warnings about volatile field used in Interlocked method
            IAppInstanceFactory factory = Interlocked.Exchange <IAppInstanceFactory>(ref m_Factory,null); 
            if (factory != null) factory.Release(this);
            #pragma warning restore 0420 //Restore supressed warning
        }

        Task<String> WaitForAccessChange(WaitTaskParams Params)
        {
            //Setup or acquire existing wait-for-change task
            if (m_Disposed) throw new ObjectDisposedException(this.GetType().Name);
            Task<String> result = null; //Resulting task to be returned
            #pragma warning disable 0420 //Supress excessive warnings about volatile field used in Interlocked methods
            OutstandingTaskInfo new_outstanding = new OutstandingTaskInfo();
            //Increment task count in advance to avoid a possibility of cleaning up the existing wait task
            //This increment accounts for the resulting task we plan to create
            Interlocked.Increment(ref m_OutstandingTaskCount);
            try 
	        {
                lock (m_WaitLock)
                {
                    //If wait task never exists or already completed an cleaned up, create new wait task
                    if (null == m_WaitTask)
                    {
                        m_WaitCts = new CancellationTokenSource();

                        //Create wait task via async call
                        m_WaitTask = StartAsyncMethod(Params, m_WaitCts.Token);

                        //Increment outstanding task count to account for guard task
                        Interlocked.Increment(ref m_OutstandingTaskCount);
                        try
                        {
                            //Attach guard task to avoid cleanup of wait task until it'll be completed
                            m_WaitTask.ContinueWith(CheckForWaitTaskCleanupAction);
                        }
                        catch
                        {
                            //Smth went wrong, so roll back outstanding task count before continuing to process the exception
                            Interlocked.Decrement(ref m_OutstandingTaskCount); //No need to check for cleanup because it still accounts for the task that was supposed to be created
                            throw;
                        }
                    }
                }
                //Prepare info for the outstanding task (resulting task to be created)
                new_outstanding.CTSource = new CancellationTokenSource(); //Create source for new resulting task cancellation token
                //Create new resulting task
                new_outstanding.Task = result = m_WaitTask.ContinueWith<String>(EndWaitForChangeFunction, new_outstanding,
                    new_outstanding.CTSource.Token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());
                //Add the task for wait task cleanup
                result.ContinueWith(CheckForWaitTaskCleanupAction);
            }
            catch
            {
                //Decrment and possibly cleanup wait task only if task creation was unsuccessful
                if (Interlocked.Decrement(ref m_OutstandingTaskCount) == 0) CleanupWaitTask();
                throw;
            }
            //Set (in a thread-safe, non-blocking manner) the new resulting task
            OutstandingTaskInfo last_outstanding = Interlocked.Exchange<OutstandingTaskInfo>(ref m_Outstanding, new_outstanding);
            //Cancel an old resulting or reconnection waiting task, if such task exist
            if (last_outstanding != null) last_outstanding.CTSource.Cancel();
            #pragma warning restore 0420 //Restore supressed warning
            return result;
        }

        public void Abandon(Task OutstandingTask)
        {
            Abandon(OutstandingTask,true);
        }
        //See if the current resulting task is the one passed as a parameter
        //If so - cancel it (if ShouldCancel specified) and clear reference to it
        //Add reconnect wait task instead (to remove this object from router list after timeout, if no reconnect)
        //The code is written as non-blocking thread-safe style using Interlocked class methods
        //because of the possible concurency with execution of WaitForChnage method in another thread
        //So it is rather tricky
        public void Abandon(Task OutstandingTask, Boolean ShouldCancel)
        {
            if(m_Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            if (null == OutstandingTask) return; //Nothing to abandon
            #pragma warning disable 0420 //Supress excessive warnings about volatile field used in Interlocked method
            //Artifically increment temporarily outstanding task count to defer wait task cleanup until we done
            Interlocked.Increment(ref m_OutstandingTaskCount);
            try 
	        {	        
                //Create outstanding info for reconnect wait task
                OutstandingTaskInfo reconnect_outstanding = new OutstandingTaskInfo();
                if (m_WaitTask != null)
                {
                    //Cancellation token source to cancel reconnect wait task
                    reconnect_outstanding.CTSource = new CancellationTokenSource();
                    //Schedule reconnect wait task, that waits RECONNECT_TIMEOUT milliseconds
                    if (m_WaitTask != null)
                        Task.Run(async () => await Task.Delay(RECONNECT_TIMEOUT, reconnect_outstanding.CTSource.Token))
                            .ContinueWith(TimeoutAction, m_WaitCts, reconnect_outstanding.CTSource.Token, 
                                TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
                }
                //Get the info on the current outstanding task and substitute reference to it (may be temporarily) by reconnect_outstanding
                // in a thread-safe manner 
                OutstandingTaskInfo last_outstanding = Interlocked.Exchange<OutstandingTaskInfo>(ref m_Outstanding, reconnect_outstanding);
                //See if the info exists... 
                if (last_outstanding != null) //Is there any outstanding task info at all..
                {
                    //...and does it contains reference to the requested task?
                    if (last_outstanding.Task == OutstandingTask)
                    {
                        //if so - Cancel the task if not completed yet and leave reconnect wait task in place
                        if (!OutstandingTask.IsCompleted && ShouldCancel) last_outstanding.CTSource.Cancel();
                    }
                    else //Info references another task, so the requested task was cancelled already
                    {
                        //Try to return info to its place in a thread-safe manner
                        if (Interlocked.CompareExchange<OutstandingTaskInfo>(ref m_Outstanding, last_outstanding, reconnect_outstanding) != reconnect_outstanding)
                            //It is another outstanding task at the place
                            last_outstanding.CTSource.Cancel(); //So its our duty to cancel the previous task
                    }
                }
	        }
	        finally
	        {
                //Decrement temporarily incremented outstanding_task_count and see if we should perform deffered cleanup of wait task
                if (Interlocked.Decrement(ref m_OutstandingTaskCount) == 0) CleanupWaitTask();
	        }           
            #pragma warning restore 0420 //Restore supressed warning
        }

        public System.Threading.Tasks.Task<string> DispatchAsync(string Json)
        {

            /*Method codes ("method_code" field)
             * -2 - Cancel()
             * -1 - ReleaseFromWait()
             *  1 - WaitForAccessChange(String Upn, int AccessToken);
             */
            if (m_Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            IdAndTheRest parse_result = Util.ExtractIntField(Json, "method_code");
            int method_code = parse_result.Id;
            //Process object-specific command
            if (1 == method_code)
            {
                //Extract parameters and call the procedure that starts the wait task
                JavaScriptSerializer jss = new JavaScriptSerializer();
                WaitTaskParams wt_params = jss.Deserialize<WaitTaskParams>(parse_result.Rest);
                return WaitForAccessChange(wt_params);
            }
            else //Process one of the common commands
            {
                OutstandingTaskInfo outstanding = m_Outstanding;
                switch (method_code)
                {
                    case CANCEL_COMMAND:
                        CancellationTokenSource cts = m_WaitCts;
                        if (!m_Disposed && cts!=null) cts.Cancel();
                        //remove this object from the list of application instaces (effectively disposing the object) in a thread-safe manner
                        #pragma warning disable 0420 //Supress excessive warnings about volatile field used in Interlocked method
                        IAppInstanceFactory factory = Interlocked.Exchange<IAppInstanceFactory>(ref m_Factory, null);
                        if (factory != null) factory.Release(this);
                        #pragma warning restore 0420 //Restore supressed warning
                        return Task<String>.FromResult("{}");
                    case ABANDON_COMMAND:
                        Task task_to_abandon = outstanding != null ? outstanding.Task : null;
                        Abandon(task_to_abandon);
                        return Task<String>.FromResult("{}");
                    default: throw new ArgumentException(String.Format("Unknown method code: {0}", method_code));
                }
            }
        }

        public bool CanRelease()
        {
            if (m_Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            bool result;
            lock (m_WaitLock) { result = null == m_WaitTask; }
            return result;
        }

        public void Dispose()
        {
            if (!m_Disposed)
            {
                m_Disposed = true;
                CleanupWaitTask();
                //Close database connection
                if (m_Conn != null) m_Conn.Dispose();
                m_Conn = null;
            }
        }

        public int Key { get { return m_Key; } }

    }
}