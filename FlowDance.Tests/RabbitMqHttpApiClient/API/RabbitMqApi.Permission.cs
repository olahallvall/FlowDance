﻿using FlowDance.Tests.RabbitMqHttpApiClient.Models.DefinitionModel;
using FlowDance.Tests.RabbitMqHttpApiClient.Utils;

namespace FlowDance.Tests.RabbitMqHttpApiClient.API
{
    public partial class RabbitMqApi//.Permission
    {
        /// <summary>
        /// A list of all permissions for all users.
        /// </summary>
        public async Task<IEnumerable<Permission>> GetPermissions()
        {
            return await DoGetCall<IEnumerable<Permission>>("/api/permissions");
        }

        /// <summary>
        /// An individual permission of a user and virtual host. 
        /// </summary>
        public async Task<Permission> GetPermissionByVhostAndUser(string virtualHost, string username)
        {
            return await DoGetCall<Permission>($"/api/permissions/{virtualHost.Encode()}/{username.Encode()}");
        }
    }
}