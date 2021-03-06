﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Batch.Common;
using Microsoft.Azure.Batch.Protocol.Models;
using Hyak.Common;
using Microsoft.Azure.Common.Authentication;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Commands.Utilities.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using BatchClient = Microsoft.Azure.Commands.Batch.Models.BatchClient;
using Microsoft.Azure.Commands.ResourceManager.Common;

namespace Microsoft.Azure.Commands.Batch
{
    public class BatchCmdletBase : AzureRMCmdlet
    {
        private BatchClient batchClient;

        public BatchClient BatchClient
        {
            get
            {
                if (batchClient == null)
                {
                    batchClient = new BatchClient(DefaultContext);
                    batchClient.VerboseLogger = WriteVerboseWithTimestamp;
                }
                return batchClient;
            }

            set { batchClient = value; }
        }

        protected virtual void OnProcessRecord()
        {
            // Intentionally left blank
        }

        protected override void ProcessRecord()
        {
            try
            {
                Validate.ValidateInternetConnection();
                ProcessRecord();
                OnProcessRecord();
            }
            catch (AggregateException ex)
            {
                // When the OM encounters an error, it'll throw an AggregateException with a nested BatchException.
                // BatchExceptions have special handling to extract detailed failure information.  When an AggregateException
                // is encountered, loop through the inner exceptions.  If there's a nested BatchException, perform the 
                // special handling.  Otherwise, just write out the error.
                AggregateException flattened = ex.Flatten();
                foreach (Exception inner in flattened.InnerExceptions)
                {
                    BatchException asBatch = inner as BatchException;
                    if (asBatch != null)
                    {
                        HandleBatchException(asBatch);
                    }
                    else
                    {
                        WriteExceptionError(inner);
                    }
                }
            }
            catch (BatchException ex)
            {
                HandleBatchException(ex);
            }
            catch (CloudException ex)
            {
                var updatedEx = ex;

                if (ex.Response != null && ex.Response.Content != null)
                {
                    var message = FindDetailedMessage(ex.Response.Content);

                    if (message != null)
                    {
                        updatedEx = new CloudException(message, ex);
                    }
                }

                WriteExceptionError(updatedEx);
            }
            catch (Exception ex)
            {
                WriteExceptionError(ex);
            }
        }

        /// <summary>
        /// For now, the 2nd message KVP inside "details" contains useful info about the failure. Eventually, a code KVP
        /// will be added such that we can search on that directly.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        internal static string FindDetailedMessage(string content)
        {
            // TODO: Revise after Task 2362107 is completed on the server side
            string message = null;

            if (CloudException.IsJson(content))
            {
                var response = JObject.Parse(content);

                // check that we have a details section
                var detailsToken = response["details"];

                if (detailsToken != null)
                {
                    var details = detailsToken as JArray;
                    if (details != null && details.Count > 1)
                    {
                        // for now, 2nd entry in array is the one we're interested in. Need a better way of identifying the
                        // detailed error message
                        var dObj = detailsToken[1] as JObject;
                        var code = dObj.GetValue("code", StringComparison.CurrentCultureIgnoreCase);
                        if (code != null)
                        {
                            message = code.ToString() + ": ";
                        }

                        var detailedMsg = dObj.GetValue("message", StringComparison.CurrentCultureIgnoreCase);
                        if (detailedMsg != null)
                        {
                            message += detailedMsg.ToString();

                        }
                    }
                }
            }

            return message;
        }

        /// <summary>
        /// Extracts failure details from the BatchException object to create a more informative error message for the user.
        /// </summary>
        /// <param name="ex">The BatchException object</param>
        private void HandleBatchException(BatchException ex)
        {
            if (ex != null)
            {
                if (ex.RequestInformation != null && ex.RequestInformation.AzureError != null)
                {
                    StringBuilder str = new StringBuilder(ex.Message).AppendLine();

                    str.AppendFormat("Error Code: {0}", ex.RequestInformation.AzureError.Code).AppendLine();
                    str.AppendFormat("Error Message: {0}", ex.RequestInformation.AzureError.Message.Value).AppendLine();
                    str.AppendFormat("Client Request ID:{0}", ex.RequestInformation.ClientRequestId).AppendLine();
                    if (ex.RequestInformation.AzureError.Values != null)
                    {
                        foreach (AzureErrorDetail detail in ex.RequestInformation.AzureError.Values)
                        {
                            str.AppendFormat("{0}:{1}", detail.Key, detail.Value).AppendLine();
                        }
                    }
                    WriteExceptionError(new BatchException(ex.RequestInformation, str.ToString(), ex.InnerException));
                }
                else
                {
                    WriteExceptionError(ex);
                }
            }
        }
    }
}
