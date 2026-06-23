namespace Framework
{
    public readonly struct AudioVolumeChangedEvent
    {
        public readonly float Master;
        public readonly float Bgm;
        public readonly float Sfx;

        public AudioVolumeChangedEvent(float master, float bgm, float sfx)
        {
            Master = master;
            Bgm = bgm;
            Sfx = sfx;
        }
    }
}
