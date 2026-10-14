namespace DomainFramework
{
    public class EntityBase
    {
        public Guid Id { get; private set; }
        public DateTime CreatedAt { get; protected set; } 
        public DateTime? UpdatedAt { get; protected set; }

        protected EntityBase()
        {
            Id = Guid.NewGuid();
        }

        protected EntityBase(Guid id)
        {
            Id = id;
        }

        protected void OnCreate()
        {
            CreatedAt = DateTime.UtcNow;
        }

        protected void OnModify()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
