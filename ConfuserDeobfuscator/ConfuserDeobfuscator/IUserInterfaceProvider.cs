namespace ConfuserDeobfuscator
{
    public interface IUserInterfaceProvider
    {
        void WriteVerbose(string data, int node = 0);
        void WriteVerbose(string formattedData, int node = 0, params object[] data);
        void Write(string data, int node = 0);
        void Write(string formattedData, int node = 0, params object[] data);
    }
}
