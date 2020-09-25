using System;
using System.Reflection;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

namespace ManMFOperator.Infrastructure
{
    //A simple alternative to full-fledged DI container
    //Implements type-resolving functionality using a (buitin) base resolver and some simple addition resolvers (the decorator pattern)
    public class DependencyResolverDecorator : IDependencyResolver 
    {
        IDependencyResolver m_BaseResolver; //Base resolver
        Dictionary<Type, Func<Object>> m_TypeCreators; //Additional resolvers list

        //Additional resolvers list element
        public struct TypeCreator
        {
            public Type aType; //Type to resolve
            public Func<Object> Creator; //Creator of an object of the specified type
        }

        //Constructor, rather straitforward
        public DependencyResolverDecorator(IDependencyResolver BaseResolver, TypeCreator[] AdditionalServices) 
        {
            m_BaseResolver = BaseResolver;
            m_TypeCreators = new Dictionary<Type, Func<Object>>();
            if(AdditionalServices!=null)
                foreach (var t in AdditionalServices)
                {
                    m_TypeCreators.Add(t.aType, t.Creator);
                }
        }

        //Auxilary method to find the creator defined in this class for the specified type (null if it isn't specified)
        Func<Object> GetCreator(Type serviceType)
        {
            if (m_TypeCreators.ContainsKey(serviceType)) return m_TypeCreators[serviceType];
            else return null;
        }

        //Implementation of IDependencyResolver.GetService method 
        public object GetService(Type serviceType)
        {
            Object result;
            Func<Object> creator = GetCreator(serviceType); //Search for an additional creator of an object of the specified type
            if (creator!=null) result=creator(); //Found: create an object
            else result = m_BaseResolver.GetService(serviceType); //Not found: try to get an object from base resolver
            return result;
        }

        //Implementation of IDependencyResolver.GetServices method 
        public System.Collections.Generic.IEnumerable<object> GetServices(Type serviceType)
        {
            //First, try to get a collection of objects from base resolver
            System.Collections.Generic.IEnumerable<object> result = m_BaseResolver.GetServices(serviceType);
            Func<Object> creator = GetCreator(serviceType); //Search for an additional creator of an object of the specified type
            if (creator != null) //Found: create an additional object
            {
                System.Collections.Generic.List<object> result_list; //We must create our own IEnumerable implementation to return
                if (null == result) result_list = new System.Collections.Generic.List<object>(); //Create a new emty list
                else result_list = new System.Collections.Generic.List<object>(result); //Copy list from the reult that the base resolver returns
                result_list.Add(creator()); //Add additional object
                return result_list;
            }
            else return result;
        }
    }
}