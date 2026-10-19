using System.Diagnostics;
namespace GameRgb;
public sealed class LightingEngine(AudioCaptureService audio,OpenRgbService rgb)
{
 public async Task RunAsync(IReadOnlyList<int> ids,int fps,Func<float> gain,Func<float> smoothing,LightingMode mode,LightingPaint paint,LightingDirection direction,Action<AudioBands> frame,CancellationToken token)
 {
  var bands=AudioBands.Silent;var clock=Stopwatch.StartNew();double previous=clock.Elapsed.TotalSeconds;
  using var timer=new PeriodicTimer(TimeSpan.FromMilliseconds(1000d/fps));
  while(await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
  {
   double now=clock.Elapsed.TotalSeconds,elapsed=Math.Min(now-previous,.25);previous=now;
   var target=Shape(audio.Analyze(gain(),0,AudioBands.Silent));bands=Follow(target,bands,(float)elapsed,smoothing());
   rgb.Update(ids,bands,mode,paint,direction);frame(bands);
  }
 }
 public async Task RunTestAsync(IReadOnlyList<int> ids,int fps,LightingMode mode,LightingPaint paint,LightingDirection direction,Action<AudioBands> frame,CancellationToken token)
 {
  var clock=Stopwatch.StartNew();using var timer=new PeriodicTimer(TimeSpan.FromMilliseconds(1000d/fps));
  while(await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
  {
   double t=clock.Elapsed.TotalSeconds;double section=t%8;float pulse=.25f+.75f*(float)((Math.Sin(t*Math.PI*4)+1)/2);
   var bands=section switch
   {
    <2=>new AudioBands(pulse,.03f,.03f,pulse),
    <4=>new AudioBands(.03f,pulse,.03f,pulse),
    <6=>new AudioBands(.03f,.03f,pulse,pulse),
    _=>new AudioBands(.55f+.35f*(float)Math.Sin(t*2),.55f+.35f*(float)Math.Sin(t*2+2.1),.55f+.35f*(float)Math.Sin(t*2+4.2),.9f)
   };
   rgb.Update(ids,bands,mode,paint,direction);frame(bands);
  }
 }
 static AudioBands Shape(AudioBands value)
 {
  float level=Gate(value.Level,.055f);if(level==0)return AudioBands.Silent;
  return new(Expand(Gate(value.Bass,.025f)),Expand(Gate(value.Mid,.025f)),Expand(Gate(value.Treble,.025f)),Expand(level));
 }
 // Increase the distance between ordinary and loud passages instead of
 // compressing everything toward full brightness. Gain lifts quiet sources;
 // this curve then restores a visible dark-to-bright pulse.
 static float Expand(float value)=>value<=0?0:MathF.Pow(Math.Clamp(value,0,1),1.7f);
 static AudioBands Follow(AudioBands target,AudioBands current,float elapsed,float smoothing)
 {
  float smooth=Math.Clamp(smoothing,0,.95f),attack=.025f+smooth*.05f,release=.06f+smooth*.3f;
  float F(float next,float old){float tau=next>old?attack:release,keep=MathF.Exp(-elapsed/tau);return next+(old-next)*keep;}
  return new(F(target.Bass,current.Bass),F(target.Mid,current.Mid),F(target.Treble,current.Treble),F(target.Level,current.Level));
 }
 static float Gate(float value,float floor)=>Math.Clamp((value-floor)/(1-floor),0,1);
}
