using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Containers.OnCollide;

public sealed class AddToContainerOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddToContainerOnCollideComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(EntityUid uid, AddToContainerOnCollideComponent component, ref StartCollideEvent args)
    {
        var currentVelocity = args.OurFixture.Body.LinearVelocity.Length;
        if (currentVelocity < component.RequiredVelocity)
            return;

        if (!_containerSystem.TryGetContainer(uid, component.Container, out var container))
            return;

        var targetBody = args.OtherFixture.Body;
        container.Insert(targetBody.Owner, EntityManager, physics: targetBody);
    }
}
