using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Pigeon_Client
{
    public class User
    {
        public string Nickname { get; set; }
        public string Login { get; set; }
        public DirectSoundOut WO { get; set; }
        public BufferedWaveProvider Buffer { get; set; }
        public VolumeSampleProvider Volume { get; set; }

        public User(string nickname, string login)
        {
            Nickname = nickname;
            Login = login;
            WO = new DirectSoundOut(Library.Latency);
            Buffer = new BufferedWaveProvider(Library.SoundFormat);
            Volume = new VolumeSampleProvider(Buffer.ToSampleProvider());
        }
    }
}