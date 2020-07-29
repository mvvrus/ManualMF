using System;

namespace AccessWaiterEP.Infrastructure.ViewModel
{
    /*
     * Return type codes ("responce_type" field)
     *  0 - normal (from API call)
     *  1 - retry
     *  
     */
    public class Return
    {
        int instance_id;
        int response_type;
        protected Return(int InstanceId, int ResponseType) { instance_id = InstanceId; response_type = ResponseType; }
    }

    public class RetryReturn:Return
    {
        public RetryReturn(int InstanceId) : base(InstanceId, 1) { }
    }

    public class ExceptionalReturn : Return
    {
        String exception_type;
        String message;
        public ExceptionalReturn(int InstanceId, Exception Ex) : base(InstanceId, 1) { 
            message = Ex.Message;
            exception_type = Ex.GetType().Name; 
        }
    }
}