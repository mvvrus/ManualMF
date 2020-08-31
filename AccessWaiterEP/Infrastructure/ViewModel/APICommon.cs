using System;

namespace AccessWaiterEP.Infrastructure.ViewModel
{
    /*
     * Return type codes ("responce_type" field)
     *  0 - normal (from API call)
     *  1 - retry
     *  2 - Exception
     *  
     */
    public class Return
    {
        int m_instance_id;
        int m_response_type;
        public int instance_id{get{return m_instance_id;}}
        public int response_type { get { return m_response_type; } }
        protected Return(int InstanceId, int ResponseType) { m_instance_id = InstanceId; m_response_type = ResponseType; }
    }

    public class RetryReturn:Return
    {
        public RetryReturn(int InstanceId) : base(InstanceId, 1) { }
    }

    public class ExceptionalReturn : Return
    {
        public String exception_type;
        public String message;
        public ExceptionalReturn(int InstanceId, Exception Ex) : base(InstanceId, 2) { 
            message = Ex.Message;
            exception_type = Ex.GetType().Name; 
        }
    }
}