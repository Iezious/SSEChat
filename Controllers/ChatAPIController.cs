using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SEEChat.Controllers
{
    [Route("/SSE")]
    public class ChatAPIController : Controller
    {
        public class ChatMessage
        {
            public string Sender { get; set; }
            public string Message { get; set; }
        }
        
        private class ActivePoint<T>
        {
            public T Value { get; set; }
        }
        
        private readonly IChatService _chatService;
        private readonly ILogger _logger;

        public ChatAPIController(IChatService chatService, ILoggerFactory logger)
        {
            _chatService = chatService;
            _logger = logger.CreateLogger("APIController");
        }

        [HttpGet]
        public async Task Index([FromQuery(Name = "name")]string username)
        {
            if (HttpContext.Request.Headers["Accept"] != "text/event-stream")
            {
                Response.StatusCode = 400;
                await Response.WriteAsync("Bad request");
                await Response.Body.FlushAsync();
                return;
            }

            Response.ContentType = "text/event-stream";
            await Response.Body.FlushAsync();

            try
            {
                var messages = new ConcurrentBag<ChatMessage>();
                ActivePoint<TaskCompletionSource<bool>> waiter = new ActivePoint<TaskCompletionSource<bool>> {Value = null};

                _chatService.Register(username, (user, message) =>
                {
                    TaskCompletionSource<bool> tr = null;

                    lock (messages)
                    {
                        messages.Add(new ChatMessage{Sender = user, Message = message});

                        if (waiter.Value != null)
                        {
                            tr = waiter.Value;
                            waiter.Value = null;
                        }
                    }

                    tr?.SetResult(true);
                });

                HttpContext.RequestAborted.Register(() =>
                {
                    TaskCompletionSource<bool> tr = null;

                    lock (messages)
                    {
                        tr = waiter.Value;
                    }

                    tr?.SetCanceled();
                });

                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    ChatMessage msg;
                    while (messages.TryTake(out msg))
                    {
                        await Response.WriteAsync($"data: {JsonConvert.SerializeObject(msg)}\n\n");
                    }
                    
                    await Response.Body.FlushAsync();

                    lock (messages)
                    {
                        waiter.Value = new TaskCompletionSource<bool>();
                    }

                    if (!await waiter.Value.Task) break;
                }
            }
            catch (Exception exx)
            {
                 _logger.LogError(-10, exx, exx.Message);               
            }
            finally
            {
                _chatService.Unregister(username);
                
            }
        }

        [HttpPost]
        public ActionResult SendMessage([FromBody] ChatMessage msg)
        {
            _chatService.Message(msg.Sender, msg.Message);
            return Ok("1");
        }
    }
}