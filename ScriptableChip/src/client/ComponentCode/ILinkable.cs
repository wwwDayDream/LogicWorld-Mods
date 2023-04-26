namespace SChipz.Client.ComponentCode
{
    public interface ILinkable
    {
        void SetLink(string Origin);
        string GetLink();
        void SetContent(byte[] ContentBytes, string Content);
        string GetContent();
    }
}