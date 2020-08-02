using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AccessWaiterEP.Infrastructure;
using AccessWaiterEP.Infrastructure.ViewModel;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using MVVrus.Utility;


namespace AccessWaiterEP
{
    public class APIHandler : HttpTaskAsyncHandler
    {
        public override async System.Threading.Tasks.Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            IRouter router = LocalRouter.GetRouter();
            IAppInstanceFactory app_instance_factory = router.GetFactory();
            IAppInstance app_instance = null;
            int instance_id = router.EmptyInstance;
            try
            {
                //Extract InstanceID and the rest of request from request
                IdAndTheRest req_data = Util.ExtractInstanceId(context.Request);
                instance_id = req_data.Id;
                Boolean no_inst_id = router.EmptyInstance == instance_id;
                if (no_inst_id)
                {
                    if (app_instance_factory != null) //Creation of dynamic application instances supported?
                        //No application instance yet. Create new dynamic instance and generate it's InstanceID
                        app_instance = app_instance_factory.CreateNewInstance(out instance_id);
                    else throw new NotImplementedException("Creation of dynamic application instances is not supported.");
                }
                else
                    //Find instance for the operation
                    app_instance = router.FindInstance(instance_id);
                if (null == app_instance)
                {
                    //Check for null app_instance
                    throw new ArgumentException("No application with this instance_id.");
                }
                String result_json;
                Task<String> result = app_instance.DispatchAsync(req_data.Rest);

                try
                {
                    if (no_inst_id && !result.IsCompleted)
                    {
                        //No instance_id was set in the request and the client should wait more (via call retry)
                        //Return retry response with generated InstanceID to the client to be used in a callretry
                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        context.Response.Write(jss.Serialize(new RetryReturn(instance_id)));
                        app_instance.Abandon(result); //Tell the application instance that our resulting task has become orphaned
                        return;
                    }


                }
                catch (Exception)
                {
                    app_instance.Abandon(result); //Tell the application instance that our resulting task has become orphaned
                    throw;
                }                
                //Wait for completion and return response from call to the client 
                result_json = await result;

                //Convert null response to an empty one
                if (null == result_json) result_json = "{}";
                //Inject response_type field value for normal return
                JsonSurgery.InjectField(result_json, "response_type", "0");
                //Inject instance_id field value
                result_json = JsonSurgery.InjectField(result_json, "instance_id", instance_id.ToString());
                context.Response.Write(result_json);
                return;
            }
            catch (HttpException h_ex)
            {
                //Special handling for HTTPException - return HTTP protocol error in the response
                context.Response.StatusCode = h_ex.GetHttpCode();
            }
            catch (Exception ex)
            {
                //Exception occured. Return exception response to the client
                JavaScriptSerializer jss = new JavaScriptSerializer();
                context.Response.Write(jss.Serialize(new ExceptionalReturn(instance_id, ex)));
                return;
            }
        }
    }
}