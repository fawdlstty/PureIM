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
						// TODO 看情况是否并入现有socket
						var _client = new ImClient { WS = _ws, UserId = _userid };
						await _client.Process ();
						//try {
						//	string _auth = _ctx.Request.Query ["auth"].ToString ();
						//	var _key = new SymmetricSecurityKey (Encoding.UTF8.GetBytes ("ddIASHDFIUABSIDABDIAfafa"));
						//	var _token_params = new TokenValidationParameters {
						//		ValidateLifetime = true,
						//		IssuerSigningKey = _key,
						//		ValidateIssuer = false,
						//		ValidateAudience = false,
						//	};
						//	var _claims = new JwtSecurityTokenHandler ().ValidateToken (_auth, _token_params, out SecurityToken _stoken);
						//	var _uid = (from p in ((JwtSecurityToken) _stoken).Claims where p.Type == "uid" select long.Parse (p.Value)).First ();
						//	string _ip = _ctx.Connection.RemoteIpAddress?.ToString () ?? "";
						//	await ClientManager.RunConnect (_uid, _ws, _ip);
						//} catch (Exception _ex) {
						//	await Log.WriteAsync (_ex);
						//	await _ws.MySendFailureAsync (-1, WsAllType.info, -1, "auth failed");
						//	await _ws.MyCloseAsync ();
						//}


						// https://docs.microsoft.com/zh-cn/aspnet/core/fundamentals/websockets?view=aspnetcore-6.0
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
