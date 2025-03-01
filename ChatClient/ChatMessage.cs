using System.Windows;

namespace ChatClient
{
    internal class ChatMessage
    {
        public string Username { get; set; }
        public string Message { get; set; }
        public string Time { get; set; }
        public string Background { get; set; }
        public string TextColor { get; set; }
        public string UsernameColor { get; set; }
        public HorizontalAlignment Alignment { get; set; }
    }
}
