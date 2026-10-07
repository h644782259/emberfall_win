# Run with Windows PowerShell (System.Speech), using the installed Mandarin voice.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$output = Join-Path (Split-Path -Parent $PSScriptRoot) 'Assets\Resources\Audio\Npc'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $voice = $synth.GetInstalledVoices() | Where-Object { $_.Enabled -and $_.VoiceInfo.Culture.Name -eq 'zh-CN' } | Select-Object -First 1
    if (-not $voice) { throw 'An installed zh-CN System.Speech voice is required.' }
    $synth.SelectVoice($voice.VoiceInfo.Name)
    $format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(22050, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
    $greetings = @(
        @{ Name='MerchantGreeting'; Rate=0; Text='欢迎，旅行者。补给都备好了，来看看吧。' },
        @{ Name='BlacksmithGreeting'; Rate=-1; Text='来得正好。把装备交给我，我帮你打磨。' },
        @{ Name='StargazerGreeting'; Rate=-1; Text='星路正在指引你。准备好下一段旅程了吗？' }
    )
    foreach ($greeting in $greetings) {
        $synth.Rate = $greeting.Rate
        $synth.SetOutputToWaveFile((Join-Path $output ($greeting.Name + '.wav')), $format)
        $synth.Speak($greeting.Text)
        $synth.SetOutputToNull()
    }
    Write-Host ('Generated three offline Mandarin greetings with ' + $voice.VoiceInfo.Name)
}
finally { $synth.Dispose() }
