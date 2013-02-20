﻿/*******************************************************************************
 * Copyright (c) 2013 IBM Corporation.
 *
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * and Eclipse Distribution License v. 1.0 which accompanies this distribution.
 *  
 * The Eclipse Public License is available at http://www.eclipse.org/legal/epl-v10.html
 * and the Eclipse Distribution License is available at
 * http://www.eclipse.org/org/documents/edl-v10.php.
 *
 * Contributors:
 *     Steve Pitschke  - initial API and implementation
 *******************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using OSLC4Net.Core.Attribute;
using OSLC4Net.Core.Model;

using log4net;

namespace OSLC4Net.Core.JsonProvider
{
    /// <summary>
    /// A class to 
    ///     - read RDF/XML from an input stream and create .NET objects.
    ///     - write .NET objects to an output stream as RDF/XML
    /// </summary>
    public class JsonMediaTypeFormatter : MediaTypeFormatter
    {

        private HttpRequestMessage httpRequest;

        /// <summary>
        /// Defauld RdfXml formatter
        /// </summary>
        public JsonMediaTypeFormatter()
        {
            SupportedMediaTypes.Add(OslcMediaType.APPLICATION_JSON_TYPE);
        }

        /// <summary>
        /// Save the HttpRequestMessage locally for use during serialization.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="request"></param>
        /// <param name="mediaType"></param>
        /// <returns></returns>
        ///
        public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
        {
            this.httpRequest = request;
            return base.GetPerRequestFormatterInstance(type, request, mediaType);
        }

        /// <summary>
        /// Test the write-ability of a type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public override bool CanWriteType(Type type)
        {
            Type actualType;

            if (ImplementsGenericType(typeof(FilteredResource<>), type))
            {
                Type[] actualTypeArguments = GetChildClassParameterArguments(typeof(FilteredResource<>), type);
            
                if (actualTypeArguments.Count() != 1)
                {
                    return false;
                }

                if (ImplementsICollection(actualTypeArguments[0]))
                {
                    actualTypeArguments = actualTypeArguments[0].GetGenericArguments();
                
                    if (actualTypeArguments.Count() != 1)
                    {
                        return false;
                    }
                
                    actualType = actualTypeArguments[0];
                }
                else
                {
                    actualType = actualTypeArguments[0];
                }
            }
            else
            {
                actualType = type;
            }
        
            if (IsSinglton(actualType))
            {
                return true;
            }
            
            return GetMemberType(type) != null;
        }

        /// <summary>
        /// Write a .NET object to an output stream
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="writeStream"></param>
        /// <param name="content"></param>
        /// <param name="transportContext"></param>
        /// <returns></returns>
        public override Task  WriteToStreamAsync(
            Type type,
            object value,
            Stream writeStream,
            HttpContent content,
            TransportContext transportContext
        )
        {
            return Task.Factory.StartNew(() =>
                {
                    JsonObject jsonObject;

                    if (ImplementsGenericType(typeof(FilteredResource<>), type))
                    {
                        PropertyInfo resourceProp = value.GetType().GetProperty("Resource");
                        Type[] actualTypeArguments = GetChildClassParameterArguments(typeof(FilteredResource<>), type);
                        object objects = resourceProp.GetValue(value, null);
                        PropertyInfo propertiesProp = value.GetType().GetProperty("Properties");

                        if (! ImplementsICollection(actualTypeArguments[0]))
                        {
                            objects = new EnumerableWrapper(objects);
                        }

                        if (ImplementsGenericType(typeof(ResponseInfo<>), type))
                        {
                            //Subject URI for the collection is the query capability
                            //TODO:  should this be set by the app based on service provider info
                            int portNum = httpRequest.RequestUri.Port;
                            string portString = null;
                            if (portNum == 80 || portNum == 443)
                            {
                                portString = "";
                            }
                            else
                            {
                                portString = ":" + portNum.ToString();
                            }

                            string descriptionAbout = httpRequest.RequestUri.Scheme + "://" +
                                                      httpRequest.RequestUri.Host +
                                                      portString +
                                                      httpRequest.RequestUri.LocalPath;

                            //Subject URI for the responseInfo is the full request URI
                            string responseInfoAbout = httpRequest.RequestUri.ToString();

                            PropertyInfo totalCountProp = value.GetType().GetProperty("TotalCount");
                            PropertyInfo nextPageProp = value.GetType().GetProperty("NextPage");

                            jsonObject = JsonHelper.CreateJson(descriptionAbout, responseInfoAbout, 
                                                               (string)nextPageProp.GetValue(value, null),
                                                               (int)totalCountProp.GetValue(value, null),
                                                               objects as IEnumerable<object>,
                                                               (IDictionary<string, object>)propertiesProp.GetValue(value, null));
                        }
                        else
                        {
                            jsonObject = JsonHelper.CreateJson(null, null, null, null, objects as IEnumerable<object>,
                                                               (IDictionary<string, object>)propertiesProp.GetValue(value, null));
                        }
                    }
                    else if (InheritedGenericInterfacesHelper.ImplementsGenericInterface(typeof(IEnumerable<>), value.GetType()))
                    {
                        jsonObject = JsonHelper.CreateJson(value as IEnumerable<object>);
                    }
                    else if (type.GetCustomAttributes(typeof(OslcResourceShape), false).Length > 0)
                    {
                        jsonObject = JsonHelper.CreateJson(new object[] { value });
                    }
                    else
                    {
                        jsonObject = JsonHelper.CreateJson(new EnumerableWrapper(value));
                    }

                    jsonObject.Save(writeStream);
                });
        }

        /// <summary>
        /// Test the readability of a type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public override bool CanReadType(Type type)
        {
            if (IsSinglton(type))
            {
                return true;
            }

            return GetMemberType(type) != null;
        }

        /// <summary>
        /// Read RDF/XML from an HTTP input stream and convert to .NET objects
        /// </summary>
        /// <param name="type"></param>
        /// <param name="readStream"></param>
        /// <param name="content"></param>
        /// <param name="formatterLogger"></param>
        /// <returns></returns>
        public override Task<object> ReadFromStreamAsync(
            Type type,
            Stream readStream,
            HttpContent content,
            IFormatterLogger formatterLogger
        )
        {
            var tcs = new TaskCompletionSource<object>();

            if (content != null && content.Headers != null && content.Headers.ContentLength == 0) return null;

            try
            {
                JsonObject jsonObject = (JsonObject)JsonObject.Load(readStream);
                bool isSingleton = IsSinglton(type);
                object output = JsonHelper.FromJson(jsonObject, isSingleton ? type : GetMemberType(type));

                if (isSingleton)
                {
                    bool haveOne = (int)output.GetType().GetProperty("Count").GetValue(output, null) > 0;

                    tcs.SetResult(haveOne ? output.GetType().GetProperty("Item").GetValue(output, new object[] { 0 }): null);
                }
                else if (type.IsArray)
                {
                    tcs.SetResult(output.GetType().GetMethod("ToArray", Type.EmptyTypes).Invoke(output, null));
                }
                else
                {
                    tcs.SetResult(output);
                }
            }
            catch (Exception e)
            {
                if (formatterLogger == null) throw;

                formatterLogger.LogError(String.Empty, e.Message);

                tcs.SetResult(GetDefaultValueForType(type));
            }

            return tcs.Task;
        }

        private bool IsSinglton(Type type)
        {
            return type.GetCustomAttributes(typeof(OslcResourceShape), false).Length > 0;
        }

        private Type GetMemberType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (InheritedGenericInterfacesHelper.ImplementsGenericInterface(typeof(IEnumerable<>), type))
            {
                Type[] interfaces = type.GetInterfaces();

                foreach (Type interfac in interfaces)
                {
                    if (interfac.IsGenericType && interfac.GetGenericTypeDefinition() == typeof(IEnumerable<object>).GetGenericTypeDefinition())
                    {
                        Type memberType = interfac.GetGenericArguments()[0];

                        if (memberType.GetCustomAttributes(typeof(OslcResourceShape), false).Length > 0)
                        {
                            return memberType;
                        }

                        return null;
                    }
                }
            }

            return null;
        }

        private static bool ImplementsGenericType(Type genericType, Type typeToTest)
        {
            bool isParentGeneric = genericType.IsGenericType;

            return ImplementsGenericType(genericType, typeToTest, isParentGeneric);
        }

        private static bool ImplementsGenericType(Type genericType, Type typeToTest, bool isParentGeneric)
        {
            if (typeToTest == null)
            {
                return false;
            }

            typeToTest = isParentGeneric && typeToTest.IsGenericType ? typeToTest.GetGenericTypeDefinition() : typeToTest;

            if (typeToTest == genericType)
            {
                return true;
            }

            return ImplementsGenericType(genericType, typeToTest.BaseType, isParentGeneric);
        }

        private static Type[] GetChildClassParameterArguments(Type genericType, Type typeToTest)
        {
            bool isParentGeneric = genericType.IsGenericType;

            while (true)
            {
                Type parentType = typeToTest.BaseType;
                Type parentToTest = isParentGeneric && parentType.IsGenericType ? parentType.GetGenericTypeDefinition() : parentType;

                if (parentToTest == genericType)
                {
                    return typeToTest.GetGenericArguments();
                }

                typeToTest = parentType;
            }
        }

        private static bool ImplementsICollection(Type type)
        {
            return type.IsGenericType && typeof(ICollection<>) == type.GetGenericTypeDefinition();
        }
    }
}