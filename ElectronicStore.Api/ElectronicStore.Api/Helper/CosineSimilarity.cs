namespace ElectronicStore.Api.Helper
{
    /// <summary>
    /// Lớp tiện ích để tính toán Cosine Similarity (Khoảng cách Cosine) giữa hai vector.
    /// Giá trị càng gần 1, hai vector càng giống nhau.
    /// </summary>
    public static class CosineSimilarity
    {
        public static double Calculate(float[] vectorA, float[] vectorB)
        {
            // Kiểm tra tính hợp lệ cơ bản của vector
            if (vectorA == null || vectorB == null || vectorA.Length != vectorB.Length || vectorA.Length == 0)
            {
                // Trả về giá trị thấp nhất (không tương đồng) nếu vector không hợp lệ
                return 0.0;
            }

            double dotProduct = 0.0; // Tích vô hướng (A . B)
            double magnitudeA = 0.0; // Độ lớn của vector A (|A|^2)
            double magnitudeB = 0.0; // Độ lớn của vector B (|B|^2)

            // Tính toán Tích vô hướng và Độ lớn (bình phương)
            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            // Tính toán Độ lớn (căn bậc hai của tổng bình phương)
            double magnitude = Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB);

            // Công thức Cosine Similarity: Cos(θ) = (A . B) / (|A| * |B|)
            if (magnitude == 0.0)
            {
                // Tránh chia cho 0. Nếu độ lớn bằng 0, vector là vector 0, độ tương đồng là 0.
                return 0.0;
            }

            return dotProduct / magnitude;
        }
    }
}
