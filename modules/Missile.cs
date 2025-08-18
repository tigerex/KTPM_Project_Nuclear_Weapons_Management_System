///Module này không có giá trị gì hết, đơn giản là để tui thử cách móc modules vô file main trên VSCode thôi.
///Hứng lên sẽ xóa

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
