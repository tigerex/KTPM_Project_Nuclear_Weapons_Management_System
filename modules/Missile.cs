namespace ProjectNuclearWeaponsManagementSystem.Modules
{
    public class Missile
    {
        public string Name { get; set; }

        public Missile(string name)
        {
            Name = name;
        }

        public void Launch()
        {
            Console.WriteLine($"{Name} launched!");
        }
    }
}
