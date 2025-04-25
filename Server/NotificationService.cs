﻿using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Org.Example.ClientFx.Grpc;

public class NotificationServiceImpl : NotificationService.NotificationServiceBase
{
    // thread-safe bag de subscriberi
    private static readonly ConcurrentBag<IServerStreamWriter<Notification>> _subscribers
        = new ConcurrentBag<IServerStreamWriter<Notification>>();

    public override async Task NewTicketBought(
        Empty request,
        IServerStreamWriter<Notification> responseStream,
        ServerCallContext context)
    {
        _subscribers.Add(responseStream);
        Console.WriteLine("Total Observers "+_subscribers.Count);
        // păstrăm metoda deschisă până clientul închide
        try {
            await Task.Delay(-1, context.CancellationToken);
        }
        catch (OperationCanceledException) { /* clientul s-a deconectat */ }
        // nu e nevoie de finally, ConcurrentBag nu suportă Remove — 
        // poți filtra când faci broadcast.
    }

    // Apelează această funcție după ce ai făcut Save() + Update() în AddTicket:
    public static async Task BroadcastAsync(FlightDTO flightDto)
    {
        Console.WriteLine("Avem de notificat "+_subscribers.Count);
        var note = new Notification { Message = 
            $"S-a cumpărat un bilet către {flightDto.Destination} la {flightDto.Date}" 
        };

        foreach (var subscriber in _subscribers) {
            try {
                Console.WriteLine("Am trimis");
                await subscriber.WriteAsync(note);
            }
            catch {
                
            }
        }
    }
}