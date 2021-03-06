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

using System;
using System.Configuration;
using Microsoft.Azure.Common.Authentication.Models;
using Microsoft.Azure.Common.Authentication.Utilities;

namespace Microsoft.Azure.Commands.Profile.Models
{
    /// <summary>
    /// Azure subscription details.
    /// </summary>
    public class PSAzureSubscription
    {
        /// <summary>
        /// Convert between formats of AzureSubscription information.
        /// </summary>
        /// <param name="other">The subscription to convert.</param>
        /// <returns>The converted subscription.</returns>
        public static implicit operator PSAzureSubscription(AzureSubscription other)
        {
            return new PSAzureSubscription
            {
                SubscriptionId = other != null? other.Id.ToString() : null,
                SubscriptionName = other != null? other.Name : null,
                TenantId = other != null && other.IsPropertySet(AzureSubscription.Property.Tenants)? 
                other.GetProperty(AzureSubscription.Property.Tenants) : null
            };
        }

        /// <summary>
        /// Convert between formats of AzureSubscription information.
        /// </summary>
        /// <param name="other">The subscription to convert.</param>
        /// <returns>The converted subscription.</returns>
        public static implicit operator AzureSubscription(PSAzureSubscription other)
        {
            if (other == null)
            {
                return null;
            }

            var result = new AzureSubscription
            {
                Name = other.SubscriptionName
            };

            if (other.SubscriptionId != null)
            {
                Guid subscriptionId;
                if (Guid.TryParse(other.SubscriptionId, out subscriptionId))
                {
                    result.Id = subscriptionId;
                }
            }

            if (other.TenantId != null)
            {
                result.Properties.SetProperty(AzureSubscription.Property.Tenants, other.TenantId);
            }

            return result;
        }

        /// <summary>
        /// The subscription id.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// The name of the subscription.
        /// </summary>
        public string SubscriptionName { get; set; }

        /// <summary>
        /// The tenant home for the subscription.
        /// </summary>
        public string TenantId { get; set; }

        public override string ToString()
        {
            return this.SubscriptionId;
        }
    }
}
