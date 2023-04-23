using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Containers.OnCollide;

public sealed class RemoveFromContainerOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RemoveFromContainerOnCollideComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, RemoveFromContainerOnCollideComponent component, ref StartCollideEvent args)
    {
        var currentVelocity = args.OurFixture.Body.LinearVelocity.Length;
        if (currentVelocity < component.RequiredVelocity)
            return;

        if (!_containerSystem.TryGetContainer(uid, component.Container, out var container))
            return;

        // remove all entities in the container
        foreach (var contained in container.ContainedEntities.ToArray())
        {
            container.Remove(contained, EntityManager);

            if (!component.RemoveEverything)
                break;
        }
    }
}
