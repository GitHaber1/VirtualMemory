using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VM_task_1
{    
    // Структура для страниц, загружаемых из файла подкачки.
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

    // Класс для управления виртуальной памятью.
    public class General
    {
        // Кол-во страниц.
        public int pageCount;
        // Список страниц.
        public List<Page> pages = new List<Page>();

        // Конструктор с параметрами.
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

        // Метод, в котором модифицируются атрибуты страницы.
        private void ModificationOfAttributes(int indexOfPage, bool state)
        {
            var t = pages[indexOfPage];
            t.time = DateTime.Now;
            pages[indexOfPage] = t;

            var s = pages[indexOfPage];
            s.status = state;
            pages[indexOfPage] = s;
        }

        // Метод, в котором самая старая страница в оперативной памяти заменяется на новую.
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

        // Метод, в котором определяется индекс страницы, на которой находится нужный элемент.
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

        // Метод, в котором заполняется битовая карта страницы.
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

        // Метод для записи заданного значения.
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

        // Метод для чтения элемента и записи его в переменную
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

        // Метод для чтения из файла.
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
            for (int i = 0; i < el.Length; i ++) 
            {
                el[i] = reader.ReadInt32();
            }
            reader.Close();
            page.symbolMap = el;

            file.Close();
            return page;
        }

        // Метод записи страницы в память.
        private void WriteToFile(Page page)  
        {
            FileStream stream = File.Open("file.bin", FileMode.Open, FileAccess.ReadWrite);
            // Смещаем указатель.
            stream.Seek(page.pageNum * 512 + page.pageNum * 64 + 2, SeekOrigin.Begin);
            // Записываем битовую карту.
            var binaryWriter = new BinaryWriter(stream);
            foreach(byte bit in page.bitMap)
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
