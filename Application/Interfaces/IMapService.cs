using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IMapService
    {
        Task<GetBuildingsResultDTO> GetBuildingsAsync(Guid cityId, int page, int pageSize);
        Task<IEnumerable<FlatDTO>> GetFlatsInBuildingAsync(Guid cityId, Guid buildingsId);
    }
}
