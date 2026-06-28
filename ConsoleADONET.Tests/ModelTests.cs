using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using ConsoleADONET.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConsoleADONET.Tests
{
    /// <summary>
    /// Чистые модульные тесты для моделей Fuel и Tank.
    /// Не требуют базы данных, выполняются всегда и детерминированно.
    ///
    /// Tank.ToString()/Fuel.ToString() форматируют числа через текущую культуру потока,
    /// поэтому в каждом тесте культура фиксируется как InvariantCulture — иначе результат
    /// зависел бы от локали машины (разделитель «.» против «,»).
    /// </summary>
    [TestClass]
    public class ModelTests
    {
        [TestInitialize]
        public void FixCulture()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        // ── Fuel ──────────────────────────────────────────────────────────

        [TestMethod]
        public void Fuel_Properties_RoundTrip()
        {
            var fuel = new Fuel { FuelId = 42, FuelType = "Бензин_1", FuelDensity = 0.72f };

            Assert.AreEqual(42, fuel.FuelId);
            Assert.AreEqual("Бензин_1", fuel.FuelType);
            Assert.AreEqual(0.72f, fuel.FuelDensity, 1e-6f);
        }

        [TestMethod]
        public void Fuel_ToString_FormatsColumnsAndFourDecimals()
        {
            var fuel = new Fuel { FuelId = 7, FuelType = "Бензин", FuelDensity = 0.75f };

            // Ожидаемый формат: FuelId в поле шириной 8 (влево), FuelType — шириной 20 (влево),
            // плотность — ровно 4 знака после запятой. Строим эталон независимо от
            // форматной строки продакшн-кода (PadRight вместо ",-8"/",-20", "0.7500" вместо ":F4").
            string expected = "7".PadRight(8) + "Бензин".PadRight(20) + "0.7500";

            Assert.AreEqual(expected, fuel.ToString());
        }

        [TestMethod]
        public void Fuel_ToString_AlwaysHasFourDecimalPlaces()
        {
            var fuel = new Fuel { FuelId = 1, FuelType = "X", FuelDensity = 1f };

            // Контракт форматирования: дробная часть — ровно 4 знака, разделитель «.».
            StringAssert.Matches(fuel.ToString(), new Regex(@"\d\.\d{4}$"));
            StringAssert.EndsWith(fuel.ToString(), "1.0000");
        }

        // ── Tank ──────────────────────────────────────────────────────────

        [TestMethod]
        public void Tank_Properties_RoundTrip()
        {
            var tank = new Tank
            {
                TankId = 5,
                TankType = "Бак_1",
                TankVolume = 50f,
                TankWeight = 120.5f,
                TankMaterial = "Сталь"
            };

            Assert.AreEqual(5, tank.TankId);
            Assert.AreEqual("Бак_1", tank.TankType);
            Assert.AreEqual(50f, tank.TankVolume, 1e-6f);
            Assert.AreEqual(120.5f, tank.TankWeight, 1e-6f);
            Assert.AreEqual("Сталь", tank.TankMaterial);
        }

        [TestMethod]
        public void Tank_ToString_FormatsAllColumns()
        {
            var tank = new Tank
            {
                TankId = 3,
                TankType = "Бак_Тест",
                TankVolume = 50f,
                TankWeight = 120.5f,
                TankMaterial = "Сталь"
            };

            // TankId(8) | TankType(20) | Volume(12, F2) | Weight(12, F2) | Material
            string expected =
                "3".PadRight(8) +
                "Бак_Тест".PadRight(20) +
                "50.00".PadRight(12) +
                "120.50".PadRight(12) +
                "Сталь";

            Assert.AreEqual(expected, tank.ToString());
        }

        [TestMethod]
        public void Tank_ToString_VolumeAndWeightUseTwoDecimals()
        {
            var tank = new Tank
            {
                TankId = 1,
                TankType = "T",
                TankVolume = 1.5f,
                TankWeight = 2f,
                TankMaterial = "M"
            };

            string result = tank.ToString();

            StringAssert.Contains(result, "1.50");
            StringAssert.Contains(result, "2.00");
            StringAssert.EndsWith(result, "M");
        }
    }
}
