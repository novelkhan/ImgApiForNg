using ImgApiForNg.DTOs.Person;
using System.Threading.Tasks;

namespace ImgApiForNg.Interfaces
{
    public interface IPersonRepository
    {
        Task<int> Save(AddPersonDTO addPersonDTO);
    }
}
