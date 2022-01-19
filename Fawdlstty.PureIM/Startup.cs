using Fawdlstty.PureIM.ImStructs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fawdlstty.PureIM {
	public class Startup {
		public Startup (IConfiguration configuration) {
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices (IServiceCollection services) {

			services.AddControllers ();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure (IApplicationBuilder app, IWebHostEnvironment env) {
			if (env.IsDevelopment ()) {
				app.UseDeveloperExceptionPage ();
			}

			// websocket
			app.UseWebSockets (new WebSocketOptions {
				KeepAliveInterval = TimeSpan.FromMinutes (1),
			});
			app.Use (async (_ctx, _next) => {
				if (_ctx.Request.Path == "/ws") {
					if (_ctx.WebSockets.IsWebSocketRequest) {
						if (!long.TryParse (_ctx.Request.Query["uid"].ToString (), out long _userid)) {
							await _ctx.Response.BodyWriter.WriteAsync (Encoding.UTF8.GetBytes (""));
							return;
						}
						string _auth = _ctx.Request.Query ["auth"].ToString ();
						// TODO check
						using var _ws = await _ctx.WebSockets.AcceptWebSocketAsync ();
						var _client = await ImManager.GetClientAsync (_userid);
						await _client.ProcessConnect (_ws);
					}
					return;
				}
				await _next ();
			});

			app.UseRouting ();

			app.UseAuthorization ();

			app.UseEndpoints (endpoints => {
				endpoints.MapControllers ();
			});
		}
	}
}
