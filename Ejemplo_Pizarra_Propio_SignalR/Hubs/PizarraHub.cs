using Grupo7_Pizarra_SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Quartz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ejemplo_Pizarra_Propio_SignalR.Hubs
{
    public class PizarraHub : Hub
    {
        private static Dictionary<string, HashSet<string>> salas = new Dictionary<string, HashSet<string>>();
        private static Dictionary<string, List<string>> dibujosPorSala = new Dictionary<string, List<string>>();
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
                dibujosPorSala[sala] = new List<string>();

                var scheduler = await _schedulerFactory.GetScheduler();
                var job = JobBuilder.Create<SignalRJob>()
                                    .WithIdentity(sala)
                                    .Build();

                var trigger = TriggerBuilder.Create()
                                            .ForJob(job)
                                            .WithIdentity($"{sala}-trigger")
                                            .WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever())
                                            .Build();

                await scheduler.ScheduleJob(job, trigger);
            }

            var usuario = Context.Items["Usuario"].ToString();
            salas[sala].Add(usuario);
            await Groups.AddToGroupAsync(Context.ConnectionId, sala);

            await Clients.Group(sala).SendAsync("UsuarioConectado", usuario);
            await EnviarDibujoActual(sala, Context.ConnectionId);
            await ActualizarUsuariosEnSala(sala);
        }

        public async Task SalirDeSala(string sala)
        {
            var usuario = Context.Items["Usuario"].ToString();
            if (salas.ContainsKey(sala))
            {
                salas[sala].Remove(usuario);
                if (salas[sala].Count == 0)
                {
                    salas.Remove(sala);
                    dibujosPorSala.Remove(sala);
                }
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sala);
            await Clients.Group(sala).SendAsync("UsuarioDesconectado", usuario);
            await ActualizarUsuariosEnSala(sala);
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            var usuario = Context.Items["Usuario"].ToString();
            foreach (var sala in salas)
            {
                if (sala.Value.Contains(usuario))
                {
                    sala.Value.Remove(usuario);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, sala.Key);
                    await Clients.Group(sala.Key).SendAsync("UsuarioDesconectado", usuario);
                    await ActualizarUsuariosEnSala(sala.Key);

                    if (sala.Value.Count == 0)
                    {
                        salas.Remove(sala.Key);
                        dibujosPorSala.Remove(sala.Key);
                    }

                    break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task Dibujar(string sala, string data)
        {
            if (dibujosPorSala.ContainsKey(sala))
            {
                dibujosPorSala[sala].Add(data);
            }
            await Clients.OthersInGroup(sala).SendAsync("dibujarEnPizarra", data);
        }

        public async Task EnviarMensaje(string sala, string message)
        {
            var usuario = Context.Items["Usuario"];
            await Clients.Group(sala).SendAsync("RecibirMensaje", usuario, message);
        }

        private async Task ActualizarUsuariosEnSala(string sala)
        {
            if (salas.ContainsKey(sala))
            {
                var usuarios = new List<string>(salas[sala]);
                await Clients.Group(sala).SendAsync("ActualizarUsuarios", usuarios);
            }
        }

        private async Task EnviarDibujoActual(string sala, string connectionId)
        {
            if (dibujosPorSala.ContainsKey(sala))
            {
                var dibujos = dibujosPorSala[sala];
                foreach (var dibujo in dibujos)
                {
                    await Clients.Client(connectionId).SendAsync("dibujarEnPizarra", dibujo);
                }
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
