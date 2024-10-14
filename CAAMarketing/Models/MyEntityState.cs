namespace CAAMarketing.Models
{
    public enum MyEntityState
    {
        //My prefered "Friendly" names for the entity states
        //     The entity is not being tracked by the context.
        Detached,
        //     The entity is being tracked by the context and exists in the database. Its property
        //     values have not changed from the values in the database.
        Unchanged,
        //     The entity is being tracked by the context and exists in the database. It has
        //     been marked for deletion from the database.
        Removed,
        //     The entity is being tracked by the context and exists in the database. Some or
        //     all of its property values have been modified.
        Updated,
        //     The entity is being tracked by the context but does not yet exist in the database.
        Added
    }
}
