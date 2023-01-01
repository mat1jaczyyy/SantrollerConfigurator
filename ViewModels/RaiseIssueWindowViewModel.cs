using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reactive;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using ReactiveUI;

namespace GuitarConfiguratorSharp.NetCore.ViewModels;

public class RaiseIssueWindowViewModel : ReactiveObject
{
    public string Text { get; }
    public string IncludedInfo { get; }
    private readonly ConfigViewModel _model;
    public ICommand RaiseIssueCommand { get; }
    public ICommand CloseWindowCommand { get; }
    public Interaction<Unit, Unit> CloseWindowInteraction = new();

    public RaiseIssueWindowViewModel((string _platformIOText, ConfigViewModel) text)
    {
        Text = text._platformIOText;
        _model = text.Item2;
        RaiseIssueCommand = ReactiveCommand.CreateFromTask(RaiseIssue);
        CloseWindowCommand = ReactiveCommand.CreateFromObservable(() => CloseWindowInteraction.Handle(new Unit()));
        var os = Environment.OSVersion;
        IncludedInfo = @$"OS Version: {os.Version}
OS Platform: {os.Platform}
OS Service Pack: {os.ServicePack}
OS VersionString: {os.VersionString}

Device Type: {_model.DeviceType}
Emulation Type: {_model.EmulationType}
Led Type: {_model.LedType}

Microcontroller Type: {_model.MicroController!.Board.Name}
Microcontroller Frequency: {_model.MicroController!.Board.CpuFreq / 1000}mhz";
    }

    private async Task RaiseIssue()
    {
        const string uri = "https://hastebin.com/documents";
        using var wc = new HttpClient();
        var postResponse = await wc.PostAsync(uri, new StringContent(Text));
        var paste = JsonNode.Parse(await postResponse.Content.ReadAsStreamAsync())?.AsObject()["key"];
        var os = Environment.OSVersion;
        var body = $@"OS Version: {os.Version}
OS Platform: {os.Platform}
OS Service Pack: {os.ServicePack}
OS VersionString: {os.VersionString}

Device Type: {_model.DeviceType}
Emulation Type: {_model.EmulationType}
Led Type: {_model.LedType}

Microcontroller Type: {_model.MicroController!.Board.Name}
Microcontroller Frequency: {_model.MicroController!.Board.CpuFreq / 1000}mhz

Compilation Log: https://hastebin.com/{paste}";
        var title = "Error building";
        body = HttpUtility.UrlEncode(body);
        title = HttpUtility.UrlEncode(title);
        var url =
            $"https://github.com/sanjay900/guitar-configurator/issues/new?title={title}&body={body}";
        Process.Start(new ProcessStartInfo {FileName = url, UseShellExecute = true});
    }
}