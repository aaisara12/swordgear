using System.Collections.Generic;
using NUnit.Framework;
using Shop;

namespace Testing
{
    [TestFixture]
    [TestOf(typeof(PurchaseUtility))]
    public class PurchaseUtilityTest
    {
        private class TestStoreItem : IStoreItem
        {
            public string Id { get; }
            public string DisplayName { get; }
            public int Cost { get; }

            public TestStoreItem(string id, int cost)
            {
                Id = id;
                Cost = cost;
            }
        }

        private class TestPurchaser : IItemPurchaser
        {
            public int WalletLedger { get; set; }
            public readonly Dictionary<string, int> Received = new Dictionary<string, int>();

            public void ReceiveItem(string id, int quantity)
            {
                if (Received.ContainsKey(id))
                    Received[id] += quantity;
                else
                    Received[id] = quantity;
            }
        }

        [Test]
        public void IsItemInStock_ReturnsTrue_WhenCountPositive()
        {
            var item = new TestStoreItem("itemA", 5);
            var available = new Dictionary<string, int> { { "itemA", 2 } };

            Assert.IsTrue(PurchaseUtility.IsItemInStock(item, available));
        }

        [Test]
        public void IsItemInStock_ReturnsFalse_WhenCountZeroOrMissing()
        {
            var item = new TestStoreItem("itemB", 1);

            var zeroAvailable = new Dictionary<string, int> { { "itemB", 0 } };
            Assert.IsFalse(PurchaseUtility.IsItemInStock(item, zeroAvailable));

            var missingAvailable = new Dictionary<string, int>();
            Assert.IsFalse(PurchaseUtility.IsItemInStock(item, missingAvailable));
        }

        [Test]
        public void IsItemReadyToPurchase_ReturnsTrue_WhenInStockAndEnoughFunds()
        {
            var item = new TestStoreItem("itemC", 10);
            var purchaser = new TestPurchaser { WalletLedger = 10 };
            var available = new Dictionary<string, int> { { "itemC", 1 } };

            Assert.IsTrue(PurchaseUtility.IsItemReadyToPurchase(item, purchaser, available));
        }

        [Test]
        public void IsItemReadyToPurchase_ReturnsFalse_WhenInsufficientFunds()
        {
            var item = new TestStoreItem("itemD", 10);
            var purchaser = new TestPurchaser { WalletLedger = 5 };
            var available = new Dictionary<string, int> { { "itemD", 1 } };

            Assert.IsFalse(PurchaseUtility.IsItemReadyToPurchase(item, purchaser, available));
        }

        [Test]
        public void IsItemReadyToPurchase_ReturnsFalse_WhenOutOfStock()
        {
            var item = new TestStoreItem("itemE", 1);
            var purchaser = new TestPurchaser { WalletLedger = 100 };
            var available = new Dictionary<string, int> { { "itemE", 0 } };

            Assert.IsFalse(PurchaseUtility.IsItemReadyToPurchase(item, purchaser, available));
        }

        [Test]
        public void TryPurchaseItem_Succeeds_ReducesWallet_AddsSingleItem_DecrementsStock()
        {
            var item = new TestStoreItem("itemF", 3);
            var purchaser = new TestPurchaser { WalletLedger = 10 };
            var available = new Dictionary<string, int> { { "itemF", 2 } };

            var result = PurchaseUtility.TryPurchaseItem(item, purchaser, available);

            Assert.IsTrue(result);
            Assert.AreEqual(7, purchaser.WalletLedger); // 10 - 3
            Assert.IsTrue(purchaser.Received.ContainsKey("itemF"));
            Assert.AreEqual(1, purchaser.Received["itemF"]);
            Assert.AreEqual(1, available["itemF"]); // 2 - 1
        }

        [Test]
        public void TryPurchaseItem_Fails_WhenInsufficientFunds_NoStateChange()
        {
            var item = new TestStoreItem("itemG", 5);
            var purchaser = new TestPurchaser { WalletLedger = 2 };
            var available = new Dictionary<string, int> { { "itemG", 3 } };

            var result = PurchaseUtility.TryPurchaseItem(item, purchaser, available);

            Assert.IsFalse(result);
            Assert.AreEqual(2, purchaser.WalletLedger);
            Assert.IsFalse(purchaser.Received.ContainsKey("itemG"));
            Assert.AreEqual(3, available["itemG"]);
        }

        [Test]
        public void TryPurchaseItem_Fails_WhenOutOfStock_NoStateChange()
        {
            var item = new TestStoreItem("itemH", 1);
            var purchaser = new TestPurchaser { WalletLedger = 10 };
            var available = new Dictionary<string, int> { { "itemH", 0 } };

            var result = PurchaseUtility.TryPurchaseItem(item, purchaser, available);

            Assert.IsFalse(result);
            Assert.AreEqual(10, purchaser.WalletLedger);
            Assert.IsFalse(purchaser.Received.ContainsKey("itemH"));
            Assert.AreEqual(0, available["itemH"]);
        }
    }
}