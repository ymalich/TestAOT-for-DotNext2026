namespace Benchmarks;

using BenchmarkDotNet.Attributes;
using System;

[MemoryDiagnoser]

public class MatrixMultiplicationBench
{
    private const int N = 512;          // 512×512 — хороший баланс (≈1–3 сек на итерацию)
                                        // private const int N = 1024;      // тяжелее, для долгого теста

    private double[,] A = new double[0, 0];
    private double[,] B = new double[0, 0];
    private double[,] C = new double[0, 0];  // результат

    [GlobalSetup]
    public void Setup()
    {
        A = CreateRandomMatrix(N);
        B = CreateRandomMatrix(N);
        C = new double[N, N];
    }

    private static double[,] CreateRandomMatrix(int size)
    {
        var rnd = new Random(42); // фиксированный сид → воспроизводимость
        var m = new double[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                m[i, j] = (rnd.NextDouble() * 200) - 100; // ±100
            }
        }

        return m;
    }

    [Benchmark]
    public void Naive()
    {
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                for (int k = 0; k < N; k++)
                {
                    C[i, j] += A[i, k] * B[k, j];
                }
            }
        }
    }

    [Benchmark]
    public void Naive_TransposedB()
    {
        // Транспонируем B → лучше локальность кэша
        var Bt = Transpose(B);

        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                double sum = 0;
                for (int k = 0; k < N; k++)
                    sum += A[i, k] * Bt[j, k];
                C[i, j] = sum;
            }
        }
    }

    private static double[,] Transpose(double[,] m)
    {
        int n = m.GetLength(0);
        var t = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                t[j, i] = m[i, j];
        return t;
    }

    // Опционально: блочная версия (cache-friendly)
    [Benchmark]
    public void Blocked()
    {
        const int BLOCK = 32; // подберите под ваш CPU (16–64)

        for (int ii = 0; ii < N; ii += BLOCK)
        {
            for (int jj = 0; jj < N; jj += BLOCK)
            {
                for (int kk = 0; kk < N; kk += BLOCK)
                {
                    int iEnd = Math.Min(ii + BLOCK, N);
                    int jEnd = Math.Min(jj + BLOCK, N);
                    int kEnd = Math.Min(kk + BLOCK, N);

                    for (int i = ii; i < iEnd; i++)
                    {
                        for (int j = jj; j < jEnd; j++)
                        {
                            double sum = C[i, j]; // читаем текущее значение
                            for (int k = kk; k < kEnd; k++)
                                sum += A[i, k] * B[k, j];
                            C[i, j] = sum;
                        }
                    }
                }
            }
        }
    }
}