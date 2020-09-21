
using System.Threading.Tasks;

namespace GSerialize
{
    public interface ISerializer
    {
        void Serialize<T>(T value);
        Task SerializeAsync<T>(T value);
        T Deserialize<T>();
        Task<T> DeserializeAsync<T>();
    }
}