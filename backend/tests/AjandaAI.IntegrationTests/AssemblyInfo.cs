// Tüm test sınıfları tek bir ajandaai_test veritabanını paylaşır; paralel TRUNCATE'ler birbirini bozar.

[assembly: CollectionBehavior(DisableTestParallelization = true)]
