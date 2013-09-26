namespace ConfuserDeobfuscator
{
    public interface IUserInterfaceProvider
    {
        void WriteVerbose(string data, int node = 0, bool nl = true);
        void WriteVerbose(string formattedData, int node = 0, bool nl = true, params object[] data);
        void Write(string data, int node = 0, bool nl = true);
        void Write(string formattedData, int node = 0, bool nl = true, params object[] data);
    }
}
