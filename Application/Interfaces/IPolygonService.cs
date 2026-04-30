using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPolygonService
    {
        Task<SavedPolygonsDto> GetAllSavedPolygonsAsync();
        Task<PolygonDto> CreateNewPolygonAsync(CreateUpdatePolygonDto createDto);
        Task<PolygonDto> UpdatePolygonAsync(Guid id, CreateUpdatePolygonDto updateDto);
        Task<bool> DeletePolygonAsync(Guid id);

    }
}
