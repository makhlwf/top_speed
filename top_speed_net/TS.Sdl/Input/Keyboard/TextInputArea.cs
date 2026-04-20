namespace TS.Sdl.Input
{
    public struct TextInputArea
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public TextInputArea(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
