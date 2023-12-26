
using System.Device.Gpio;
using System.Diagnostics;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;

namespace Lights.Web.AddHostedService;

public class LightService : IHostedService
{
    public GpioController Controller { get; }
    public List<int> Pins { get; } = new List<int>(new[]{
        26,
        19,
        13,
        6,
        5,
        11,
        9,
        10
    });
    private CancellationTokenSource Token { get; }
    protected Playback? Playback { get; set; }
    public bool Play { get; set; }

    public LightService()
    {
        Controller = new GpioController();
        Token = new CancellationTokenSource();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var task = RunLights();
        if (task.IsCompleted)
        {
            return task;
        }

        return Task.CompletedTask;
    }

    public async Task RunLights()
    {
        await Startup();
        await PlayShow();
    }

    public async Task PlayShow()
    {
        var midiFile = MidiFile.Read(
            "test.mid",
            new ReadingSettings
            {
                NoHeaderChunkPolicy = NoHeaderChunkPolicy.Abort,
            });

        var tempMap = midiFile.GetTempoMap();
        var notes = midiFile.GetNotes()
            .Select(n => new { n.Time, Start = true, Note = n })
            .Union(midiFile.GetNotes().Select(n => new { Time = n.EndTime, Start = false, Note = n }))
            .OrderBy(n => n.Time)
            .ToList();
        Play = true;
        var stopwatch = new Stopwatch();
        while (!Token.Token.IsCancellationRequested)
        {
            stopwatch.Restart();
            foreach (var note in notes)
            {
                if (Token.Token.IsCancellationRequested)
                {
                    break;
                }

                var timeToPlay = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempMap);
                var sleepTime = timeToPlay.TotalMilliseconds - stopwatch.ElapsedMilliseconds;
                if (sleepTime > 0)
                {
                    await Task.Delay((int)sleepTime);
                }

                Controller.Write(Pins[note.Note.Channel], note.Start ? PinValue.Low : PinValue.High);
            }

            // We're going to await a tiny bit between plays allowing for the CPU to reclaim some time if it's hammering notes for interrupts
            await Task.Delay(500);
        }
    }

    public async Task Startup()
    {
        Pins.ForEach(p => Controller.OpenPin(p, PinMode.Output));
        foreach (var pin in Pins)
        {

            await Task.Delay(200);
            Controller.Write(pin, PinValue.Low);
        }

        Pins.ForEach(p => Controller.Write(p, PinValue.High));
        await Task.Delay(500);
        Pins.ForEach(p => Controller.Write(p, PinValue.Low));
        await Task.Delay(500);
        Pins.ForEach(p => Controller.Write(p, PinValue.High));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Playback?.Stop();
        Token.Cancel();
        await Task.Delay(1000);
        Playback?.Dispose();
        Pins.ForEach(p => Controller.Write(p, PinValue.High));
        Pins.ForEach(p => Controller.ClosePin(p));
    }
}

