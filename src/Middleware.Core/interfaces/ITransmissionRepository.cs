using Middleware.Core.Models;  

namespace Middleware.Core.interfaces;

public interface ITransmissionRepository
{
    void Add(Transmission transmission);
}