using Bogus.DataSets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal static class Faker
{
    private static Random _random = new Random(420);

    private static string[] _names = new string[]
    {
        "Tainara", "Marques", "Martine", "Lucas", "Reginaldo", "Flores", "Jr.", "Ariadna", "Flávia", "Branco", "Malena", "Cortês", "Estrada",
        "Catarina", "Rosa", "Carrara", "Fernandes", "Sérgio", "Ávila", "Sobrinho", "Aurélio", "Helder", "Rico", "Neto", "Eduardo", "Fernando", "Beltrão",
        "Diogo", "Táles", "Neves", "Serra", "Tiago", "Wesley", "Carrara", "Ferminiano", "Rafael", "Tomáz", "Ávila", "Campos", "Nicole", "Aguiar", "dos", "Santos",
        "Tânia", "Bittencourt", "Sá", "Regina", "Vanda", "Corona", "Linhares", "Demian", "Jardel", "Espinoza", "Galvão", "Nóbrega", "Guilherme", "Juliano", "Colaço", "Madeira",
        "Bianca", "Abgail", "Delatorre", "Solano", "Banhos", "Michele", "Catarina", "Maldonado", "Jácomo", "Delgado", "Cristina", "Aranda", "Danilo", "Kim", "Camacho", "Roque", "dos", "Santos",
        "Luciano", "Pedrosa", "Sandoval", "Ziraldo", "Brito", "Álvares", "Leo", "Renato", "Jimenes", "Sobrinho", "Melissa", "Bonilha", "Grego", "Inácio", "Nilton", "Cruz", "Padilha",
        "Abgail", "Lígia", "Carmona", "Madeira", "Lins", "Fernando", "Lucas", "Colaço", "Lozano", "Laerte", "Flores", "Eunice", "Estrada", "Albina", "Dias", "Faro",
        "Joaquin", "Ciro", "Balestero", "Rios", "Rafael", "Cícero", "Galindo", "Lucas", "Raí", "Lutero", "Filho", "Kevin", "Osório", "Fidalgo", "Berenice", "Caldeira", "Lovato",
        "Maria", "Flores", "Gusmões","Carla", "Emília", "Maldonado", "Romero",  "Luara", "Bittencourt", "Ferminiano", "Quitéria", "Rosimeire", "Cortês", "Kevin", "Helder",
        "Igor", "Barros", "Joyce", "Tábata", "Ávila", "Costa", "Clotilde", "Janete", "Campos", "Marés", "Cardoso", "Ana", "Ferreira", "Marin",
        "Gislaine", "Balestero", "Correia", "Gilmar", "Wagner", "Caldeira", "Lutero", "Altino", "Heitor", "Carmona", "Daniel", "Rogério", "Aragão",
        "Hugo", "Branco", "Sobrinho", "Madeira", "Santacruz"
    };

    // _nomes masculino (primeiro nome) 1000
    // _nomes femininos 1000
    // _sobrenomes 1000

    private static string[] _lorem = new string[] { "Lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "Suspendisse", "tempus", "sapien", "maximus", "dictum", "pretium", "tellus", "dui", "tincidunt", "massa", "lobortis", "pellentesque", "dolor", "ligula", "et", "nulla" };

    public static string Fullname()
    {
        return _names[_random.Next(_names.Length - 1)] + " " + _names[_random.Next(_names.Length - 1)];
    }

    public static int Age()
    {
        return _random.Next(18, 96);
    }

    public static DateTime Birthday()
    {
        var oldest = DateTime.Today.AddYears(-110).Ticks;
        var now = DateTime.Now.Ticks;
        var range =  now - oldest;

        var date = new DateTime(oldest + _random.NextLong(0, range));

        return date;
    }

    public static string Lorem(int size, int end = -1)
    {
        return string.Join(" ", Enumerable.Range(1, end == -1 ? size : _random.Next(size, end))
            .Select(x => _lorem[_random.Next(_lorem.Length - 1)]));
    }

    public static int Next(int start, int end)
    {
        return _random.Next(start, end);
    }

    // https://stackoverflow.com/a/13095144/3286260
    public static long NextLong(this Random random, long min, long max)
    {
        if (max <= min)
            throw new ArgumentOutOfRangeException("max", "max must be > min!");

        //Working with ulong so that modulo works correctly with values > long.MaxValue
        ulong uRange = (ulong)(max - min);

        //Prevent a modolo bias; see https://stackoverflow.com/a/10984975/238419
        //for more information.
        //In the worst case, the expected number of calls is 2 (though usually it's
        //much closer to 1) so this loop doesn't really hurt performance at all.
        ulong ulongRand;
        do
        {
            byte[] buf = new byte[8];
            random.NextBytes(buf);
            ulongRand = (ulong)BitConverter.ToInt64(buf, 0);
        } while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);

        return (long)(ulongRand % uRange) + min;
    }
}
