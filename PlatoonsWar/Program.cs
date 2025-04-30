namespace PlatoonsWar
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<Solider> soliderReferences = new()
            {
                new Solider(100, 60, 50),
                new Sniper(1.6f, 100, 45, 45),
                new Stormtrooper(5, 100, 40, 45),
                new Supporter(5, 100, 35, 40),
            };

            SoldiersClonner soldiersClonner = new();
            List<Solider> attackers = soldiersClonner.CloneSoliders(soliderReferences, 5);
            List<Solider> defenders = soldiersClonner.CloneSoliders(soliderReferences, 6);

            Platoon attackerPlatoon = new(attackers, "Зайчики");
            Platoon defenderPlatoon = new(defenders, "Пантеры");

            Battlefield battlefield = new(attackerPlatoon, defenderPlatoon);
            BattlefieldMenu menu = new(battlefield);

            menu.ExecuteFight();
            Console.ReadKey();
        }
    }

    public static class UserUtilits
    {
        private static Random s_random = new();

        public static int GetRandomNumber(int minValue, int maxValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxValue, minValue);

            return s_random.Next(minValue, maxValue);
        }

        public static int GetRandomNumber(int maxValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValue);

            return s_random.Next(maxValue);
        }
    }

    public class SoldiersClonner
    {
        public List<Solider> CloneSoliders(List<Solider> references, int clonesCount)
        {
            ArgumentNullException.ThrowIfNull(references);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clonesCount);

            List<Solider> clonedSoliders = new();

            foreach (var solider in references)
            {
                for (int i = 0; i < clonesCount; i++)
                {
                    clonedSoliders.Add(solider.Clone());
                }
            }

            return clonedSoliders;
        }
    }

    public class BattlefieldMenu
    {
        private readonly Battlefield _battlefield;

        public BattlefieldMenu(Battlefield battlefield)
        {
            ArgumentNullException.ThrowIfNull(battlefield);

            _battlefield = battlefield;
        }

        public void ExecuteFight()
        {
            Console.Clear();
            ShowPlatoons();
            Console.ReadKey();

            while (_battlefield.PlatoonsCanFight)
            {
                Console.Clear();
                _battlefield.ExecuteAttack();
                ShowPlatoons();
                _battlefield.SwitchPlatoons();
                Console.ReadKey();
            }

            Console.WriteLine($"\nВзвод \"{_battlefield.Defender.Name}\" победил");
        }

        private void ShowPlatoonInfo(IPlatoon platoon)
        {
            Console.WriteLine($"Имя взвода: {platoon.Name}");
            Console.WriteLine("Здоровье солдат:");

            foreach (var solider in platoon.Soliders)
            {
                Console.WriteLine(solider.Health);
            }
        }

        private void ShowPlatoons()
        {
            Console.WriteLine("Атакующие:");
            ShowPlatoonInfo(_battlefield.Attacker);
            Console.WriteLine("\nОбороняющиеся:");
            ShowPlatoonInfo(_battlefield.Defender);
        }
    }

    public class Battlefield
    {
        private Platoon _attacker;
        private Platoon _defender;

        public Battlefield(Platoon attacker, Platoon defender)
        {
            ArgumentNullException.ThrowIfNull(attacker);
            ArgumentNullException.ThrowIfNull(defender);

            _attacker = attacker;
            _defender = defender;
        }

        public IPlatoon Attacker => _attacker;
        public IPlatoon Defender => _defender;

        public bool PlatoonsCanFight => _attacker.HaveSoliders && _defender.HaveSoliders;

        public void ExecuteAttack()
        {
            if (PlatoonsCanFight == false)
            {
                throw new InvalidOperationException("Attack or defend platoon is empty");
            }

            _attacker.Attack(new List<IDamageble>(_defender.Soliders));
        }

        public void SwitchPlatoons()
        {
            Platoon temp = _attacker;
            _attacker = _defender;
            _defender = temp;
        }
    }

    public class Platoon : IPlatoon
    {
        private readonly List<Solider> _soliders;

        public Platoon(List<Solider> soliders, string name)
        {
            ArgumentNullException.ThrowIfNull(soliders);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(name);

            _soliders = soliders;
            Name = name;

            foreach (var solider in _soliders)
            {
                solider.Died += RemoveSolider;
            }
        }

        public string Name { get; }

        public IReadOnlyList<IDamageble> Soliders => _soliders;

        public bool HaveSoliders => Soliders.Count > 0;

        public void Attack(List<IDamageble> enemys)
        {
            foreach (var solider in _soliders)
            {
                if (enemys.Count > 0)
                {
                    solider.Attack(new List<IDamageble>(enemys));
                }
            }
        }

        private void RemoveSolider(Solider solider)
        {
            _soliders.Remove(solider);
            solider.Died -= RemoveSolider;
        }
    }

    public interface IPlatoon
    {
        string Name { get; }

        IReadOnlyList<IDamageble> Soliders { get; }
    }

    public interface IDamageble
    {
        public int Health { get; }

        void TakeDamage(int damage);
    }

    public class Solider : IDamageble
    {
        public event Action<Solider> Died;

        public Solider(int health, int armor, int damage)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(health);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(armor);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(damage);

            Health = health;
            Armor = armor;
            Damage = damage;
        }

        public int Health { get; private set; }

        protected int Armor { get; }

        protected int Damage { get; }

        public bool IsDied => Health <= 0;

        public virtual void Attack(List<IDamageble> enemys)
        {
            int targetIndex = UserUtilits.GetRandomNumber(enemys.Count);
            IDamageble target = enemys[targetIndex];
            target.TakeDamage(Damage);
        }

        public void TakeDamage(int damage)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(damage);

            int absorbedDamage = (int)(damage * (damage / (float)(damage + Armor)));
            Health = Math.Max(Health - absorbedDamage, 0);

            if (IsDied)
            {
                Died?.Invoke(this);
            }
        }

        public virtual Solider Clone() => new Solider(Health, Armor, Damage);
    }

    public class Sniper : Solider
    {
        private readonly float _damageMultiplyer;

        public Sniper(float damageMultiplyer, int health, int armor, int damage) : base(health, armor, damage)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(damageMultiplyer, 1);

            _damageMultiplyer = damageMultiplyer;
        }

        public override void Attack(List<IDamageble> enemys)
        {
            int targetIndex = UserUtilits.GetRandomNumber(enemys.Count);
            IDamageble target = enemys[targetIndex];
            target.TakeDamage((int)(Damage * _damageMultiplyer));
        }

        public override Solider Clone() => new Sniper(_damageMultiplyer, Health, Armor, Damage);
    }

    public class Stormtrooper : Solider
    {
        public Stormtrooper(int attacksCount, int health, int armor, int damage) : base(health, armor, damage)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attacksCount);

            AttacksCount = attacksCount;
        }

        protected int AttacksCount { get; }

        public override void Attack(List<IDamageble> enemys)
        {
            int attacksCount = Math.Min(AttacksCount, enemys.Count);

            for (int i = 0; i < attacksCount; i++)
            {
                int targetIndex = UserUtilits.GetRandomNumber(enemys.Count);
                IDamageble target = enemys[targetIndex];
                target.TakeDamage(Damage);
            }
        }

        public override Solider Clone() => new Stormtrooper(AttacksCount, Health, Armor, Damage);
    }

    public class Supporter : Stormtrooper
    {
        public Supporter(int attacksCount, int health, int armor, int damage) : base(attacksCount, health, armor, damage)
        {

        }

        public override void Attack(List<IDamageble> enemys)
        {
            int attacksCount = Math.Min(AttacksCount, enemys.Count);

            for (int i = 0; i < attacksCount; i++)
            {
                int targetIndex = UserUtilits.GetRandomNumber(enemys.Count);
                IDamageble target = enemys[targetIndex];
                target.TakeDamage(Damage);
                enemys.Remove(target);
            }
        }

        public override Solider Clone() => new Supporter(AttacksCount, Health, Armor, Damage);
    }
}
