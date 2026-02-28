namespace FeedbackTriageVectorSearch.Services;

public static class VectorMath
{
    public static float CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length != right.Length)
        {
            throw new ArgumentException("Vector dimensions must match for cosine similarity.");
        }

        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (var i = 0; i < left.Length; i++)
        {
            var l = left[i];
            var r = right[i];

            dot += l * r;
            leftNorm += l * l;
            rightNorm += r * r;
        }

        var denom = Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm);
        if (denom <= 0)
        {
            return 0;
        }

        return (float)(dot / denom);
    }

    public static float[] Clone(float[] vector)
    {
        var copy = new float[vector.Length];
        Array.Copy(vector, copy, vector.Length);
        return copy;
    }
}
