using System.Threading.Tasks;
using NServiceBus.Internal.Projections;

namespace NServiceBus.Internal
{
    public interface IRegisterProjections
    {
        Task RegisterProjectionsFor(IProjectionsManager projectionsManager);
    }
}