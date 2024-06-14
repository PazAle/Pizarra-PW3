using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Quartz;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Grupo7_Pizarra_SignalR;

namespace Ejemplo_Pizarra_Propio_SignalR.Hubs
{
    public class PizarraHub : Hub
    {
        private static Dictionary<string, HashSet<string>> salas = new Dictionary<string, HashSet<string>>();
        private readonly ISchedulerFactory _schedulerFactory;

        public PizarraHub(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }
        public async Task UnirseASala(string sala)
        {
            if (!salas.ContainsKey(sala))
            {
                salas[sala] = new HashSet<string>();

                var scheduler = await _schedulerFactory.GetScheduler();
                var job = JobBuilder.Create<SignalRJob>()
                                    .WithIdentity(sala)
                                    .Build();

                var trigger = TriggerBuilder.Create()
                                            .ForJob(job)
                                            .WithIdentity($"{sala}-trigger")
                                            .WithCronSchedule("0 0/1 * * * ?")
                                            .Build();

                await scheduler.ScheduleJob(job, trigger);

            }

            //salas[sala].Add(Context.ConnectionId);
            salas[sala].Add(Context.Items["Usuario"].ToString());
            await Groups.AddToGroupAsync(Context.ConnectionId, sala);

            await Clients.Group(sala).SendAsync("UsuarioConectado", Context.ConnectionId);
            await ActualizarUsuariosEnSala(sala);
        }

        public async Task SalirDeSala(string sala)
        {
            if (salas.ContainsKey(sala))
            {
                salas[sala].Remove(Context.ConnectionId);
                if (salas[sala].Count == 0)
                {
                    salas.Remove(sala);
                }
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sala);
            await Clients.Group(sala).SendAsync("UsuarioDesconectado", Context.ConnectionId);
            await ActualizarUsuariosEnSala(sala);
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            foreach (var sala in salas)
            {
                if (sala.Value.Contains(Context.ConnectionId))
                {
                    sala.Value.Remove(Context.ConnectionId);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, sala.Key);
                    await Clients.Group(sala.Key).SendAsync("UsuarioDesconectado", Context.ConnectionId);
                    await ActualizarUsuariosEnSala(sala.Key);

                    if (sala.Value.Count == 0)
                    {
                        salas.Remove(sala.Key);
                    }

                    break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task Dibujar(string sala, string data)
        {
            await Clients.OthersInGroup(sala).SendAsync("dibujarEnPizarra", data);
        }

        public async Task EnviarMensaje(string sala, string message)
        {
            var user = Context.Items["Usuario"];
            //var user = Context.User.Identity.Name ?? "Anonimo";
            await Clients.Group(sala).SendAsync("RecibirMensaje", user, message);
        }

        private async Task ActualizarUsuariosEnSala(string sala)
        {
            if (salas.ContainsKey(sala))
            {
                var usuarios = new List<string>(salas[sala]);
                await Clients.Group(sala).SendAsync("ActualizarUsuarios", usuarios);
            }
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var usuario = httpContext.Session.GetString("nombre");

            if (!string.IsNullOrEmpty(usuario))
            {
                Context.Items["Usuario"] = usuario;
            }

            await base.OnConnectedAsync();
        }

    }
}
