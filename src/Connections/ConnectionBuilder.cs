﻿using System;
using System.Collections.Generic;
using System.Text;
using Apache.Cassandra;

namespace FluentCassandra.Connections
{
	public class ConnectionBuilder
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="keyspace"></param>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="timeout"></param>
		public ConnectionBuilder(string keyspace, string host, int port = Server.DefaultPort, int connectionTimeout = Server.DefaultTimeout, bool pooling = false, int minPoolSize = 0, int maxPoolSize = 100, int connectionLifetime = 0, string username = null, string password = null)
		{
			Keyspace = keyspace;
			Servers = new List<Server>() { new Server(host, port) };
			ConnectionTimeout = connectionTimeout;
			Pooling = pooling;
			MinPoolSize = minPoolSize;
			MaxPoolSize = maxPoolSize;
			ConnectionLifetime = connectionLifetime;
			ConnectionString = GetConnectionString();
			ReadConsistency = ConsistencyLevel.QUORUM;
			WriteConsistency = ConsistencyLevel.QUORUM;
			Username = username;
			Password = password;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="connectionString"></param>
		public ConnectionBuilder(string connectionString)
		{
			InitializeConnectionString(connectionString);
			ConnectionString = GetConnectionString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="connectionString"></param>
		private void InitializeConnectionString(string connectionString)
		{
			string[] connParts = connectionString.Split(';');
			IDictionary<string, string> pairs = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (string part in connParts)
			{
				string[] nameValue = part.Split(new[] { '=' }, 2);

				if (nameValue.Length != 2)
					continue;

				pairs.Add(nameValue[0], nameValue[1]);
			}

			#region Keyspace

			if (pairs.ContainsKey("Keyspace"))
			{
				Keyspace = pairs["Keyspace"];
			}

			#endregion

			#region ConnectionTimeout

			if (!pairs.ContainsKey("Connection Timeout"))
			{
				ConnectionTimeout = 0;
			}
			else
			{
				int connectionTimeout;

				if (!Int32.TryParse(pairs["Connection Timeout"], out connectionTimeout))
					throw new CassandraException("Connection Timeout is not valid.");

				if (connectionTimeout < 0)
					connectionTimeout = 0;
				
				ConnectionTimeout = connectionTimeout;
			}

			#endregion

			#region Server

			Servers = new List<Server>();

			if (!pairs.ContainsKey("Server"))
			{
				Servers.Add(new Server());
			}
			else
			{
				string[] servers = pairs["Server"].Split(',');
				foreach (var server in servers)
				{
					string[] serverParts = server.Split(':');
					string host = serverParts[0];

					if (serverParts.Length == 2)
					{
						int port;
						if (Int32.TryParse(serverParts[1], out port))
							Servers.Add(new Server(host: host, port: port, timeout: ConnectionTimeout));
						else
							Servers.Add(new Server(host: host, timeout: ConnectionTimeout));
					}
					else
						Servers.Add(new Server(host));
				}
			}

			#endregion

			#region Pooling

			if (!pairs.ContainsKey("Pooling"))
			{
				Pooling = false;
			}
			else
			{
				bool pooling;

				if (!Boolean.TryParse(pairs["Pooling"], out pooling))
					pooling = false;

				Pooling = pooling;
			}

			#endregion

			#region MinPoolSize

			if (!pairs.ContainsKey("Min Pool Size"))
			{
				MinPoolSize = 0;
			}
			else
			{
				int minPoolSize;

				if (!Int32.TryParse(pairs["Min Pool Size"], out minPoolSize))
					minPoolSize = 0;

				if (minPoolSize < 0)
					minPoolSize = 0;

				MinPoolSize = minPoolSize;
			}

			#endregion

			#region MaxPoolSize

			if (!pairs.ContainsKey("Max Pool Size"))
			{
				MaxPoolSize = 100;
			}
			else
			{
				int maxPoolSize;

				if (!Int32.TryParse(pairs["Max Pool Size"], out maxPoolSize))
					maxPoolSize = 100;

				if (maxPoolSize < 0)
					maxPoolSize = 100;

				MaxPoolSize = maxPoolSize;
			}

			#endregion

			#region ConnectionLifetime

			if (!pairs.ContainsKey("Connection Lifetime"))
			{
				ConnectionLifetime = 0;
			}
			else
			{
				int lifetime;

				if (!Int32.TryParse(pairs["Connection Lifetime"], out lifetime))
					lifetime = 0;

				ConnectionLifetime = lifetime;
			}

			#endregion

			#region Read

			if (!pairs.ContainsKey("Read"))
			{
				ReadConsistency = ConsistencyLevel.QUORUM;
			}
			else
			{
				ConsistencyLevel read;

				if (!Enum.TryParse(pairs["Read"], out read))
					ReadConsistency = ConsistencyLevel.QUORUM;

				ReadConsistency = read;
			}

			#endregion

			#region Write

			if (!pairs.ContainsKey("Write"))
			{
				WriteConsistency = ConsistencyLevel.QUORUM;
			}
			else
			{
				ConsistencyLevel write;

				if (!Enum.TryParse(pairs["Write"], out write))
					WriteConsistency = ConsistencyLevel.QUORUM;

				WriteConsistency = write;
			}

			#endregion

			#region Username

			if (pairs.ContainsKey("Username"))
			{
				Username = pairs["Username"];
			}

			#endregion

			#region Password

			if (pairs.ContainsKey("Password"))
			{
				Password = pairs["Password"];
			}

			#endregion
		}

		private string GetConnectionString()
		{
			StringBuilder b = new StringBuilder();
			string format = "{0}={1};";

			b.AppendFormat(format, "Keyspace", Keyspace);
			b.AppendFormat(format, "Server", String.Join(",", Servers));
			b.AppendFormat(format, "Connection Timeout", ConnectionTimeout);
			b.AppendFormat(format, "Pooling", Pooling);
			b.AppendFormat(format, "Min Pool Size", MinPoolSize);
			b.AppendFormat(format, "Max Pool Size", MaxPoolSize);
			b.AppendFormat(format, "Connection Lifetime", ConnectionLifetime);
			b.AppendFormat(format, "Read", ReadConsistency);
			b.AppendFormat(format, "Write", WriteConsistency);
			b.AppendFormat(format, "Username", Username);
			b.AppendFormat(format, "Password", Password == null ? "" : new String('X', Password.Length));

			return b.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		public string Keyspace { get; private set; }

		/// <summary>
		/// The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.
		/// </summary>
		public int ConnectionTimeout { get; private set; }

		/// <summary>
		/// When true, the Connection object is drawn from the appropriate pool, or if necessary, is created and added to the appropriate pool. Recognized values are true, false, yes, and no.
		/// </summary>
		public bool Pooling { get; private set; }

		/// <summary>
		/// (Not Currently Implimented) The minimum number of connections allowed in the pool.
		/// </summary>
		public int MinPoolSize { get; private set; }

		/// <summary>
		/// The maximum number of connections allowed in the pool.
		/// </summary>
		public int MaxPoolSize { get; private set; }

		/// <summary>
		/// When a connection is returned to the pool, its creation time is compared with the current time, and the connection is destroyed if that time span (in seconds) exceeds the value specified by Connection Lifetime. This is useful in clustered configurations to force load balancing between a running server and a server just brought online. A value of zero (0) causes pooled connections to have the maximum connection timeout.
		/// </summary>
		public int ConnectionLifetime { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public ConsistencyLevel ReadConsistency { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public ConsistencyLevel WriteConsistency { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public IList<Server> Servers { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public string Username { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public string Password { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public string ConnectionString { get; private set; }
	}
}