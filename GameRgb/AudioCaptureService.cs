using System.Numerics;
using System.Runtime.InteropServices;
using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;
namespace GameRgb;
public sealed class AudioCaptureService:IAsyncDisposable
{
 const int Rate=48000,Channels=2,Size=2048;
 readonly object gate=new(); readonly float[] ring=new float[Size]; int write,count;long lastPacketTimestamp;WasapiRecorder? recorder;
 public async Task StartAsync(int? processId)
 {
  await StopAsync();
  var b=new WasapiRecorderBuilder().WithFormat(WaveFormat.CreateIeeeFloatWaveFormat(Rate,Channels));
  recorder=processId is int pid?await b.WithProcessLoopback((uint)pid,ProcessLoopbackMode.IncludeTargetProcessTree).BuildAsync():b.WithLoopbackCapture().Build();
  recorder.DataAvailable+=OnData; recorder.StartRecording();
 }
 public AudioBands Analyze(float gain,float smoothing,AudioBands old)
 {
  long last=Volatile.Read(ref lastPacketTimestamp);
  if(last==0||Stopwatch.GetElapsedTime(last)>TimeSpan.FromMilliseconds(250))return Mix(AudioBands.Silent,old,smoothing);
  var input=new float[Size];
  lock(gate){if(count<Size/4)return Mix(AudioBands.Silent,old,smoothing);for(int i=0;i<Size;i++)input[i]=ring[(write+i)%Size];}
  var fft=new Complex[Size];double squares=0;
  for(int i=0;i<Size;i++){squares+=input[i]*input[i];double w=.5-.5*Math.Cos(2*Math.PI*i/(Size-1));fft[i]=new(input[i]*w,0);}
  Transform(fft);
  static float C(double v,float g)=>(float)Math.Clamp(Math.Log10(1+v*g*18)/Math.Log10(19),0,1);
  return Mix(new(C(Energy(fft,35,220),gain),C(Energy(fft,220,2000),gain),C(Energy(fft,2000,12000),gain),C(Math.Sqrt(squares/Size),gain)),old,smoothing);
 }
 public async Task StopAsync()
 {
  var r=Interlocked.Exchange(ref recorder,null);if(r is null)return;r.DataAvailable-=OnData;
  try{r.StopRecording();}catch{} await r.DisposeAsync(); lock(gate){Array.Clear(ring);write=count=0;}Volatile.Write(ref lastPacketTimestamp,0);
 }
 public async ValueTask DisposeAsync()=>await StopAsync();
 void OnData(ReadOnlySpan<byte> data,AudioClientBufferFlags flags,long pos,long qpc)
 {
  Volatile.Write(ref lastPacketTimestamp,Stopwatch.GetTimestamp());
  if((flags&AudioClientBufferFlags.Silent)!=0)
  {
   int frames=data.Length/(sizeof(float)*Channels);
   lock(gate)for(int i=0;i<frames;i++){ring[write]=0;write=(write+1)%Size;count=Math.Min(count+1,Size);}
   return;
  }
  var s=MemoryMarshal.Cast<byte,float>(data);
  lock(gate)for(int i=0;i+1<s.Length;i+=Channels){var mono=(s[i]+s[i+1])*.5f;ring[write]=float.IsFinite(mono)?mono:0;write=(write+1)%Size;count=Math.Min(count+1,Size);}
 }
 static AudioBands Mix(AudioBands n,AudioBands o,float smooth){var k=Math.Clamp(smooth,0,.95f);var a=1-k;return new(o.Bass*k+n.Bass*a,o.Mid*k+n.Mid*a,o.Treble*k+n.Treble*a,o.Level*k+n.Level*a);}
 static double Energy(Complex[] f,double low,double high)
 {
  int a=Math.Max(1,(int)(low*Size/Rate)),b=Math.Min(Size/2-1,(int)(high*Size/Rate));double power=0;
  for(int i=a;i<=b;i++)power+=f[i].Real*f[i].Real+f[i].Imaginary*f[i].Imaginary;
  // Transform() is intentionally unscaled. For a Hann-windowed real signal, a
  // bin-centred sine has a positive-frequency magnitude of about amplitude*N/4.
  // Convert the band's root-sum-square magnitude back to input amplitude so the
  // following gain curve receives a stable 0..1-ish value instead of FFT-size values.
  return Math.Sqrt(power)*4/Size;
 }
 static void Transform(Complex[] v)
 {
  int n=v.Length;for(int i=1,j=0;i<n;i++){int bit=n>>1;for(;(j&bit)!=0;bit>>=1)j^=bit;j^=bit;if(i<j)(v[i],v[j])=(v[j],v[i]);}
  for(int len=2;len<=n;len<<=1){double angle=-2*Math.PI/len;var step=new Complex(Math.Cos(angle),Math.Sin(angle));for(int off=0;off<n;off+=len){var weight=Complex.One;for(int j=0;j<len/2;j++){var even=v[off+j];var odd=v[off+j+len/2]*weight;v[off+j]=even+odd;v[off+j+len/2]=even-odd;weight*=step;}}}
 }
}
