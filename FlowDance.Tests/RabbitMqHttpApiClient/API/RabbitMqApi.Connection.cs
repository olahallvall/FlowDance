﻿using FlowDance.Tests.RabbitMqHttpApiClient.Models.ConnectionModel;
using FlowDance.Tests.RabbitMqHttpApiClient.Utils;

namespace FlowDance.Tests.RabbitMqHttpApiClient.API
{
    public partial class RabbitMqApi//.Connection
    {
		/// <summary>
        /// A list of all open connections.
        /// </summary>
        public async Task<IEnumerable<Connection>> GetConnections()
        {
            return await DoGetCall<IEnumerable<Connection>>("/api/connections");
        }

        /// <summary>
        /// A list of all open connections in a specific vhost.
        /// </summary>
        public async Task<IEnumerable<Connection>> GetConnectionsByVhost(string virtualHost)
        {
            return await DoGetCall<IEnumerable<Connection>>($"/api/vhosts/{virtualHost.Encode()}/connections");
        }

		/// <summary>
        /// An individual connection by connection name
        /// </summary>
        public async Task<Connection> GetConnection(string connectionName)
		{
            return await DoGetCall<Connection>($"/api/connections/{connectionName.Encode()}");
        }
    }
}