using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VM_task_1
{
    public class Enter
    {
        // Метод, в котором моделируется управление виртуальной памятью.
        private void TestRun(General gen)
        {
            Random rand = new Random();
            int index;
            for (int i = 0; i < 3; i++)
            {
                index = rand.Next(gen.pageCount + 1);
                gen.pages.Add(gen.ReadFromFile(index, File.Open("file.bin", FileMode.Open, FileAccess.ReadWrite)));
            }
            // Переменная для случайного заполнения ячеек.
            int randomSym;
            // Заполняем отедльные ячейки.
            if (gen.WriteElement(522, 120) == -1)
                Console.WriteLine("Ошибка записи!");
            if (gen.WriteElement(523, 120) == -1)
                Console.WriteLine("Ошибка записи!");
            if (gen.WriteElement(555, 120) == -1)
                Console.WriteLine("Ошибка записи!");
            if (gen.WriteElement(530, 120) == -1)
                Console.WriteLine("Ошибка записи!");
            // Проверка, если недостаточно памяти для размещения элемента.
            if (gen.WriteElement(10500, 120) == -1)
                Console.WriteLine("Ошибка записи!");
            // Для наглядности заполняем подряд 4 страницы.
            for (int i = 0; i < 512; i++)
            {
                randomSym = rand.Next(2, 1000);
                // Вывод элемента массива до записи в него.
                if (gen.ReadElement(i) == -1)
                    Console.WriteLine("Ошибка чтения!");
                else
                    Console.WriteLine(gen.ReadElement(i) + " - до записи");
                if (gen.WriteElement(i, randomSym) == -1)
                    Console.WriteLine("Ошибка записи!");
                // Вывод элемента после записи в него.
                if (gen.ReadElement(i) == -1)
                    Console.WriteLine("Ошибка чтения!");
                else
                    Console.WriteLine(gen.ReadElement(i) + " - после записи");
            }
        }
        // Точка входа в программу.
        public static void Main()
        {
            General gen = new General("file.bin", 10000);
            Enter enter = new Enter();
            enter.TestRun(gen);
        }
    }
}
