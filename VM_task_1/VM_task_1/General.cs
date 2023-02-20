using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VM_task_1
{
    /// <summary>
    /// Структура для страниц, загружаемых из файла подкачки.
    /// </summary>
    public struct Page
    {
        // Порядковый номер страницы в памяти.
        public int pageNum;
        // Индикатор статуса страницы (false - страница не модифицировалась, true - была запись).
        public bool status;
        // Время записи страницы в память.
        public DateTime time;
        // Битовая карта страницы.
        public byte[] bitMap;
        // Массив значений моделируемого массива, находящихся на странице.
        public int[] symbolMap;
        public Page()
        {
            pageNum = 0;
            status = false;
            time = DateTime.Now;
            bitMap = new byte[64];
            symbolMap = new int[128];
        }
    }

    /// <summary>
    /// Класс для управления виртуальной памятью.
    /// </summary>
    public class General
    {
        // Кол-во страниц.
        public int pageCount;
        // Список страниц.
        public List<Page> pages = new List<Page>();

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        /// <param name="path"> Путь к указанному файлу. </param>
        /// <param name="size"> Заданное пользователем кол-во элементов в файле. </param>
        public General(string path, int size)
        {
            // Рассчитываем кол-во страниц и округляем.
            pageCount = (int)Math.Ceiling((decimal)size * sizeof(int) / 512);

            // Проверяем, существует ли указанный файл.
            if (!File.Exists(path))
            {
                // Создаем файл. 
                var stream = File.Create(path);
                byte[] buffer = Encoding.Default.GetBytes("VM");
                // Записываем сигнатуру.
                stream.Write(buffer, 0, buffer.Length);
                buffer = new byte[512 + 64];
                for (int i = 0; i < buffer.Length; i++)
                {
                    // Записываем необходимое кол-во нулей на страницу.
                    buffer[i] = 0x00000000;
                }
                // Проверяем, достаточно ли памяти на диске для файла подкачки.
                try
                {
                    for (int i = 0; i < pageCount; i++)
                    {
                        // Заполняем нулями страницы.
                        stream.Write(buffer, 0, buffer.Length);
                    }
                }
                // Если недостаточно, то завершаем работу.
                catch (System.IO.IOException e)
                {
                    Console.WriteLine(e.ToString);
                    System.Threading.Thread.Sleep(10000);
                    System.Environment.Exit(1);
                }
                stream.Close();
            }


        }

        /// <summary>
        /// Метод, в котором модифицируются атрибуты страницы.
        /// </summary>
        /// <param name="indexOfPage"> Индекс страницы. </param>
        /// <param name="state"> Статус страницы. </param>
        private void ModificationOfAttributes(int indexOfPage, bool state)
        {
            var t = pages[indexOfPage];
            t.time = DateTime.Now;
            pages[indexOfPage] = t;

            var s = pages[indexOfPage];
            s.status = state;
            pages[indexOfPage] = s;
        }

        /// <summary>
        /// Метод, в котором самая старая страница в оперативной памяти заменяется на новую.
        /// </summary>
        /// <param name="indexOfPage"> Индекс страницы. </param>
        /// <returns> Возвращается индекс новой страницы. </returns>
        private int ReplacePage(int indexOfPage)
        {
            int oldPageIndex = 0;
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].time < pages[oldPageIndex].time)
                    oldPageIndex = i;
            }

            if (pages[oldPageIndex].status)
            {
                WriteToFile(pages[oldPageIndex]);
            }

            pages[oldPageIndex] = ReadFromFile(indexOfPage, File.Open("file.bin", FileMode.Open, FileAccess.ReadWrite));
            indexOfPage = oldPageIndex;
            return indexOfPage;

        }

        /// <summary>
        /// Метод, в котором определяется индекс страницы, на которой находится нужный элемент.
        /// </summary>
        /// <param name="indexOfElement"> Индекс элемента в файле. </param>
        /// <returns> Возвращается номер страницы, на которой находится нужный элемент. </returns>
        private int IdentificatePage(long indexOfElement)
        {
            // Находим, на какой странице находится элемент.
            int indexOfPage = (int)(indexOfElement / 128);

            if (indexOfElement > pageCount * 128)
                return -1;

            int currentPageIndex = -1;

            // Смотрим, загружена ли в память данная страница.
            for (int i = 0; i < pages.Count; i++)
            {
                if (pages[i].pageNum == indexOfPage)
                {
                    currentPageIndex = i;
                    break;
                }
            }

            // Если страница не загружена в память, то выбираем самую старую страницу.
            if (currentPageIndex == -1)
            {
                indexOfPage = ReplacePage(indexOfPage);
            }
            else
                indexOfPage = currentPageIndex;

            // Модифицируем атрибуты страницы.
            ModificationOfAttributes(indexOfPage, false);

            return indexOfPage;
        }

        /// <summary>
        /// Метод, в котором заполняется битовая карта страницы.
        /// </summary>
        /// <param name="indexOfPage"> Индекс страницы. </param>
        /// <param name="pageIndexOfElement"> Индекс элемента на странице. </param>
        private void FillingInTheBitmap(int indexOfPage, int pageIndexOfElement)
        {
            if (pages[indexOfPage].symbolMap[pageIndexOfElement] != 0)
                pages[indexOfPage].bitMap[pageIndexOfElement / 2] |= (1 << 0);
            else
                pages[indexOfPage].bitMap[pageIndexOfElement / 2] |= (0 << 0);
            if (pages[indexOfPage].symbolMap[pageIndexOfElement + 1] != 0)
                pages[indexOfPage].bitMap[pageIndexOfElement / 2] |= (1 << 4);
            else
                pages[indexOfPage].bitMap[pageIndexOfElement / 2] |= (0 << 4);
        }

        /// <summary>
        /// Метод для записи заданного значения.
        /// </summary>
        /// <param name="indexOfElement"> Индекс элемента в файле. </param>
        /// <param name="copy"> Значение, которое нужно записать. </param>
        /// <returns> Возвращается либо 0, либо -1 (которая сигнализирует об ошибке). </returns>
        public int WriteElement(int indexOfElement, int copy)
        {
            // Определяем номер страницы.
            int indexOfPage = IdentificatePage(indexOfElement);

            if (indexOfPage == -1)
                return -1;

            // Находим индекс элемента на странице.
            int pageIndexOfElement = (int)(indexOfElement % 128);
            pages[indexOfPage].symbolMap[pageIndexOfElement] = copy;

            // Модифицируем атрибуты страницы.
            ModificationOfAttributes(indexOfPage, true);

            if (pageIndexOfElement % 2 != 0)
            {
                pageIndexOfElement -= 1;
            }
            // Заполняем битовую карту.
            FillingInTheBitmap(indexOfPage, pageIndexOfElement);

            WriteToFile(pages[indexOfPage]);

            return 0;
        }

        /// <summary>
        /// Метод для чтения элемента и записи его в переменную
        /// </summary>
        /// <param name="indexOfElement"> Индекс элемента на странице. </param>
        /// <returns> Возвращается число, считанное из файла. </returns>
        public int ReadElement(int indexOfElement)
        {
            // Определяем номер страницы. 
            int indexOfPage = IdentificatePage(indexOfElement);
            if (indexOfPage > pageCount || indexOfPage == -1)
            {
                return -1;
            }

            // Определяем индекс элемента на странице.
            int pageIndexOfElement = (int)(indexOfElement % 128);
            // Записываем элемент в переменную. 
            int returnValue = pages[indexOfPage].symbolMap[pageIndexOfElement];
            return returnValue;
        }

        /// <summary>
        /// Метод для чтения из файла.
        /// </summary>
        /// <param name="pageNum"> Номер страницы. </param>
        /// <param name="file"> Поток, используемый для чтения из файла. </param>
        /// <returns> Возвращается страница, считанная из файла. </returns>
        public Page ReadFromFile(int pageNum, FileStream file)
        {
            Page page = new Page();
            page.pageNum = pageNum;

            long pointer = (long)pageNum * 512 + (long)pageNum * 64 + 64 + 2;
            // Сдвигаем указатель в файле на нужное кол-во элементов.
            file.Seek(pointer, SeekOrigin.Begin);
            int[] el = new int[128];
            var reader = new BinaryReader(file);
            // Копируем считанные элементы в массив integer.
            for (int i = 0; i < el.Length; i++)
            {
                el[i] = reader.ReadInt32();
            }
            reader.Close();
            page.symbolMap = el;

            file.Close();
            return page;
        }

        /// <summary>
        /// Метод записи страницы в память.
        /// </summary>
        /// <param name="page"> Страница, загруженная в оперативную память. </param>
        private void WriteToFile(Page page)
        {
            FileStream stream = File.Open("file.bin", FileMode.Open, FileAccess.ReadWrite);
            // Смещаем указатель.
            stream.Seek(page.pageNum * 512 + page.pageNum * 64 + 2, SeekOrigin.Begin);
            // Записываем битовую карту.
            var binaryWriter = new BinaryWriter(stream);
            foreach (byte bit in page.bitMap)
            {
                binaryWriter.Write(bit);
            }
            // Записываем массив значений, находящихся на странице.
            foreach (int num in page.symbolMap)
            {
                binaryWriter.Write(num);
            }

            binaryWriter.Close();

            stream.Close();
        }
    }
}
