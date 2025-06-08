using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Data.SQLite;
using System.Security.Cryptography;
using WpfApp1.Models;

namespace WpfApp1
{
    /// <summary>
    /// Lớp thực hiện thuật toán băm SHA3-512
    /// </summary>
    public class SHA3_512
    {
        // Các tham số cố định cho SHA3-512
        // Rate: số byte xử lý mỗi vòng (576 bits = 72 bytes)
        // Capacity: phần còn lại của trạng thái (1024 bits = 128 bytes)
        // OutputLength: độ dài đầu ra (512 bits = 64 bytes)
        private static readonly int Rate = 576 / 8; // Tốc độ xử lý (576 bits)
        private static readonly int Capacity = 1024 / 8; // Dung lượng (1024 bits)
        private static readonly int OutputLength = 512 / 8; // Độ dài đầu ra (512 bits)

        // Các hằng số vòng lặp cho thuật toán Keccak
        // Mỗi vòng lặp Keccak sử dụng một hằng số riêng để tăng tính bảo mật
        private static readonly ulong[] RoundConstants = new ulong[]
        {
            0x0000000000000001UL, 0x0000000000008082UL, 0x800000000000808AUL, 0x8000000080008000UL,
            0x000000000000808BUL, 0x0000000080000001UL, 0x8000000080008081UL, 0x8000000000008009UL,
            0x000000000000008AUL, 0x0000000000000088UL, 0x0000000080008009UL, 0x000000008000000AUL,
            0x000000008000808BUL, 0x800000000000008BUL, 0x8000000000008089UL, 0x8000000000008003UL,
            0x8000000000008002UL, 0x8000000000000080UL, 0x000000000000800AUL, 0x800000008000000AUL,
            0x8000000080008081UL, 0x8000000000008080UL, 0x0000000080000001UL, 0x8000000080008008UL
        };

        // Ma trận dịch chuyển cho phép biến đổi Rho
        // RhoOffsets xác định số bit dịch chuyển cho từng phần tử trạng thái
        private static readonly int[,] RhoOffsets = new int[5, 5]
        {
            { 0, 1, 62, 28, 27 },
            { 36, 44, 6, 55, 20 },
            { 3, 10, 43, 25, 39 },
            { 41, 45, 15, 21, 8 },
            { 18, 2, 61, 56, 14 }
        };

        // Ma trận hoán vị cho phép biến đổi Pi
        // PiPermutations xác định vị trí mới của từng phần tử trạng thái sau khi hoán vị
        private static readonly int[,] PiPermutations = new int[5, 5]
        {
            { 0, 3, 1, 4, 2 },
            { 1, 4, 2, 0, 3 },
            { 2, 0, 3, 1, 4 },
            { 3, 1, 4, 2, 0 },
            { 4, 2, 0, 3, 1 }
        };

        /// <summary>
        /// Thực hiện phép xoay trái bit
        /// </summary>
        // Hàm này xoay trái một số nguyên 64 bit n vị trí
        private static ulong RotateLeft(ulong x, int n) => (x << n) | (x >> (64 - n));

        /// <summary>
        /// Thực hiện phép biến đổi Keccak-F trên trạng thái
        /// </summary>
        // Hàm này thực hiện 24 vòng biến đổi Keccak-F trên ma trận trạng thái 5x5
        // Bao gồm các bước: Theta, Rho, Pi, Chi, Iota
        private static void KeccakF(ulong[,] state)
        {
            for (int round = 0; round < 24; round++)
            {
                // Bước Theta: Kết hợp các bit từ các cột
                ulong[] C = new ulong[5];
                for (int x = 0; x < 5; x++)
                    C[x] = state[x, 0] ^ state[x, 1] ^ state[x, 2] ^ state[x, 3] ^ state[x, 4];
                for (int x = 0; x < 5; x++)
                {
                    ulong D = C[(x + 4) % 5] ^ RotateLeft(C[(x + 1) % 5], 1);
                    for (int y = 0; y < 5; y++)
                        state[x, y] ^= D;
                }

                // Bước Rho và Pi: Xoay và hoán vị các bit
                ulong[,] temp = new ulong[5, 5];
                for (int x = 0; x < 5; x++)
                    for (int y = 0; y < 5; y++)
                        temp[PiPermutations[x, y] % 5, (x + 3 * y) % 5] = RotateLeft(state[x, y], RhoOffsets[x, y]);

                // Bước Chi: Áp dụng phép biến đổi phi tuyến
                for (int x = 0; x < 5; x++)
                    for (int y = 0; y < 5; y++)
                        state[x, y] = temp[x, y] ^ (~temp[(x + 1) % 5, y] & temp[(x + 2) % 5, y]);

                // Bước Iota: Thêm hằng số vòng lặp
                state[0, 0] ^= RoundConstants[round];
            }
        }

        /// <summary>
        /// Tính toán giá trị băm SHA3-512 cho dữ liệu đầu vào
        /// </summary>
        // Hàm này nhận vào mảng byte dữ liệu, trả về mảng byte băm SHA3-512
        public static byte[] ComputeHash(byte[] data)
        {
            int blockSize = Rate;
            byte[] padded = PadData(data, blockSize);
            ulong[,] state = new ulong[5, 5];
            int stateSize = 1600 / 8; // 1600 bits = 200 bytes

            // Xử lý từng khối dữ liệu
            for (int i = 0; i < padded.Length; i += blockSize)
            {
                byte[] block = padded.Skip(i).Take(blockSize).ToArray();
                for (int j = 0; j < blockSize / 8; j++)
                {
                    int x = j % 5;
                    int y = j / 5;
                    byte[] lane = block.Skip(j * 8).Take(8).ToArray();
                    state[x, y] ^= BitConverter.ToUInt64(lane, 0);
                }
                KeccakF(state);
            }

            // Tạo giá trị băm đầu ra
            byte[] hash = new byte[OutputLength];
            int offset = 0;
            for (int y = 0; y < 5 && offset < OutputLength; y++)
                for (int x = 0; x < 5 && offset < OutputLength; x++)
                {
                    byte[] lane = BitConverter.GetBytes(state[x, y]);
                    Array.Copy(lane, 0, hash, offset, Math.Min(8, OutputLength - offset));
                    offset += 8;
                }
            return hash.Take(OutputLength).ToArray();
        }

        /// <summary>
        /// Thêm padding cho dữ liệu đầu vào
        /// </summary>
        // Hàm này thêm padding theo chuẩn SHA3 cho dữ liệu đầu vào
        private static byte[] PadData(byte[] data, int blockSize)
        {
            int dataLen = data.Length;
            int paddingLen = blockSize - (dataLen % blockSize);
            byte[] padded = new byte[dataLen + paddingLen];
            Array.Copy(data, padded, dataLen);
            padded[dataLen] = 0x06; // Padding cho SHA3
            padded[padded.Length - 1] |= 0x80;
            return padded;
        }
    }

    /// <summary>
    /// Lớp thực hiện thuật toán mã hóa và chữ ký số ElGamal
    /// </summary>
    // Lớp này hiện thực các hàm tạo khóa, ký, xác thực, lưu trữ, và quản lý bài nộp sử dụng ElGamal
    public class ElGamal
    {
        /// <summary>
        /// Tính lũy thừa modulo (base^exp mod mod)
        /// </summary>
        // Hàm này tính (baseValue^exp) mod mod bằng phương pháp bình phương và nhân
        public static BigInteger PowerMod(BigInteger baseValue, BigInteger exp, BigInteger mod)
        {
            BigInteger result = 1;
            baseValue %= mod;
            while (exp > 0)
            {
                if ((exp & 1) == 1)
                    result = (result * baseValue) % mod;
                exp >>= 1;
                baseValue = (baseValue * baseValue) % mod;
            }
            return result;
        }

        /// <summary>
        /// Tính nghịch đảo modulo (a^(-1) mod m)
        /// </summary>
        // Tìm số nghịch đảo của a theo modulo m (tức là tìm x sao cho a*x ≡ 1 mod m)
        public static BigInteger ModInverse(BigInteger a, BigInteger m)
        {
            BigInteger m0 = m;
            BigInteger y = 0, x = 1;
            if (m == 1) return 0;
            while (a > 1)
            {
                BigInteger q = a / m;
                BigInteger t = m;
                m = a % m;
                a = t;
                t = y;
                y = x - q * y;
                x = t;
            }
            if (x < 0) x += m0;
            return x;
        }

        /// <summary>
        /// Kiểm tra số nguyên tố
        /// </summary>
        // Kiểm tra một số nguyên n có phải là số nguyên tố hay không (dùng phương pháp chia thử)
        public static bool IsPrime(BigInteger n)
        {
            if (n < 2) return false;
            for (BigInteger i = 2; i * i <= n; i++)
                if (n % i == 0) return false;
            return true;
        }

        /// <summary>
        /// Tạo số nguyên tố ngẫu nhiên trong khoảng [start, end]
        /// </summary>
        // Sinh số nguyên tố ngẫu nhiên trong đoạn [start, end] bằng cách thử liên tục
        public static BigInteger GeneratePrime(int start, int end)
        {
            Random rnd = new Random();
            while (true)
            {
                BigInteger candidate = rnd.Next(start, end);
                if (IsPrime(candidate)) return candidate;
            }
        }

        /// <summary>
        /// Tính ước chung lớn nhất của hai số
        /// </summary>
        // Tìm ước chung lớn nhất (GCD) của hai số a và b
        public static BigInteger GreatestCommonDivisor(BigInteger a, BigInteger b)
        {
            while (b != 0)
            {
                BigInteger temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        /// <summary>
        /// Tạo cặp khóa ElGamal
        /// </summary>
        // Sinh ngẫu nhiên một cặp khóa ElGamal: p (nguyên tố lớn), g (sinh), x (bí mật), h (công khai)
        public static (BigInteger p, BigInteger g, BigInteger h, BigInteger x) GenerateKeys()
        {
            BigInteger p = GeneratePrime(2000000, 3000000);
            Random rnd = new Random();
            BigInteger g = rnd.Next(2, (int)p - 1);
            BigInteger x = rnd.Next(1, (int)(p - 2));
            BigInteger h = PowerMod(g, x, p);
            return (p, g, h, x);
        }

        /// <summary>
        /// Ký tin nhắn sử dụng khóa riêng
        /// </summary>
        // Ký dữ liệu (data) bằng khóa bí mật x, trả về chữ ký (r, s)
        public static (BigInteger r, BigInteger s) SignMessage(BigInteger p, BigInteger g, BigInteger x, byte[] data)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                BigInteger k = 0;
                BigInteger r = 0, s = 0;
                do
                {
                    // Tạo số ngẫu nhiên k
                    byte[] randomBytes = new byte[32];
                    rng.GetBytes(randomBytes);
                    k = BigInteger.Abs(new BigInteger(randomBytes)) % (p - 3) + 2;
                    if (GreatestCommonDivisor(k, p - 1) != 1) continue;
                    r = PowerMod(g, k, p);

                    // Tính giá trị băm của dữ liệu
                    byte[] hashBytes = SHA3_512.ComputeHash(data);
                    BigInteger h = new BigInteger(hashBytes.Concat(new byte[] { 0 }).ToArray()) % (p - 1);
                    if (h < 0) h += p - 1;

                    // Tính chữ ký s
                    BigInteger k_inv = ModInverse(k, p - 1);
                    BigInteger temp = (h - (x * r) % (p - 1));
                    if (temp < 0) temp += (p - 1);
                    temp = temp % (p - 1);
                    s = (k_inv * temp) % (p - 1);
                } while (r == 0 || s == 0 || r >= p || s >= p - 1);
                return (r, s);
            }
        }

        /// <summary>
        /// Xác thực chữ ký số
        /// </summary>
        // Kiểm tra chữ ký (r, s) có hợp lệ với dữ liệu và khóa công khai không
        public static bool VerifySignature(BigInteger p, BigInteger g, BigInteger h, byte[] data, BigInteger r, BigInteger s, byte[] providedHash)
        {
            if (r <= 0 || r >= p || s <= 0 || s >= p - 1)
                return false;

            // Kiểm tra giá trị băm
            byte[] computedHash = SHA3_512.ComputeHash(data);
            if (!computedHash.SequenceEqual(providedHash))
                return false;

            BigInteger h_msg = new BigInteger(providedHash.Concat(new byte[] { 0 }).ToArray()) % (p - 1);
            if (h_msg < 0) h_msg += p - 1;

            // Xác thực chữ ký
            BigInteger v1_part1 = PowerMod(h, r, p);
            BigInteger v1_part2 = PowerMod(r, s, p);
            BigInteger v1 = (v1_part1 * v1_part2) % p;
            BigInteger v2 = PowerMod(g, h_msg, p);
            return v1 == v2;
        }

        /// <summary>
        /// Ký và lưu file với thông tin sinh viên
        /// </summary>
        // Ký file, lưu chữ ký, hash, dữ liệu file, và thông tin sinh viên vào file và cơ sở dữ liệu
        public static void SignAndSaveFile(string fileName, (BigInteger p, BigInteger g, BigInteger h, BigInteger x)? privateKeySender, string outputPath, int studentId, string studentName, string className, string subjectName)
        {
            if (privateKeySender == null)
                throw new ArgumentNullException(nameof(privateKeySender), "Private key cannot be null");

            var (p, g, h, x) = privateKeySender.Value;
            byte[] data = File.ReadAllBytes(fileName);
            byte[] hash = SHA3_512.ComputeHash(data);
            var (r, s) = SignMessage(p, g, x, data);

            // Lưu thông tin file
            List<string> lines = new List<string>
            {
                Path.GetExtension(fileName),
                Convert.ToBase64String(hash),
                $"Signature: {r},{s}",
                Convert.ToBase64String(data)
            };
            File.WriteAllLines(outputPath, lines);

            // Lưu thông tin vào cơ sở dữ liệu
            using (var conn = new SQLiteConnection("Data Source=submissions.db;Version=3;"))
            {
                conn.Open();
                var command = new SQLiteCommand(
                    "INSERT OR REPLACE INTO Students (Id, Name, PublicKeyP, PublicKeyG, PublicKeyH) VALUES (@Id, @Name, @P, @G, @H)",
                    conn);
                command.Parameters.AddWithValue("@Id", studentId);
                command.Parameters.AddWithValue("@Name", studentName);
                command.Parameters.AddWithValue("@P", p.ToString());
                command.Parameters.AddWithValue("@G", g.ToString());
                command.Parameters.AddWithValue("@H", h.ToString());
                command.ExecuteNonQuery();

                command = new SQLiteCommand(
                    "INSERT INTO Submissions (StudentId, FileName, Hash, SignatureR, SignatureS, SubmissionTime, Status, FilePath, ClassName, SubjectName) VALUES (@StudentId, @FileName, @Hash, @SignatureR, @SignatureS, @SubmissionTime, @Status, @FilePath, @ClassName, @SubjectName)",
                    conn);
                command.Parameters.AddWithValue("@StudentId", studentId);
                command.Parameters.AddWithValue("@FileName", Path.GetFileName(fileName));
                command.Parameters.AddWithValue("@Hash", Convert.ToBase64String(hash));
                command.Parameters.AddWithValue("@SignatureR", r.ToString());
                command.Parameters.AddWithValue("@SignatureS", s.ToString());
                command.Parameters.AddWithValue("@SubmissionTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@Status", "Pending");
                command.Parameters.AddWithValue("@FilePath", outputPath);
                command.Parameters.AddWithValue("@ClassName", className ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SubjectName", subjectName ?? (object)DBNull.Value);
                command.ExecuteNonQuery();
                conn.Close();
            }
        }

        /// <summary>
        /// Xác thực và giải nén file
        /// </summary>
        // Đọc file, xác thực chữ ký, giải nén dữ liệu gốc ra file mới nếu hợp lệ
        public static bool VerifyAndExtractFile(string fileName, (BigInteger p, BigInteger g, BigInteger h)? publicKeySender, string outputPathWithoutExt, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (publicKeySender == null)
            {
                errorMessage = "Public key cannot be null";
                return false;
            }

            var (p, g, h) = publicKeySender.Value;
            var lines = File.ReadAllLines(fileName);
            if (lines.Length < 4)
            {
                errorMessage = "Invalid file format";
                return false;
            }

            // Đọc thông tin file
            string extension = lines[0];
            byte[] providedHash;
            try
            {
                providedHash = Convert.FromBase64String(lines[1]);
            }
            catch
            {
                errorMessage = "Invalid hash format";
                return false;
            }

            // Đọc chữ ký
            var signatureParts = lines[2].Replace("Signature: ", "").Split(',');
            if (signatureParts.Length != 2)
            {
                errorMessage = "Invalid signature format";
                return false;
            }

            if (!BigInteger.TryParse(signatureParts[0].Trim(), out BigInteger r) ||
                !BigInteger.TryParse(signatureParts[1].Trim(), out BigInteger s))
            {
                errorMessage = "Failed to parse signature components";
                return false;
            }

            // Đọc dữ liệu file
            byte[] data;
            try
            {
                data = Convert.FromBase64String(lines[3]);
            }
            catch
            {
                errorMessage = "Invalid file data format";
                return false;
            }

            // Xác thực chữ ký
            if (!VerifySignature(p, g, h, data, r, s, providedHash))
            {
                errorMessage = "Signature verification failed or hash mismatch";
                return false;
            }

            // Giải nén file
            string outputPath = outputPathWithoutExt + extension;
            File.WriteAllBytes(outputPath, data);
            return true;
        }

        /// <summary>
        /// Lấy danh sách các bài nộp
        /// </summary>
        // Truy vấn toàn bộ bảng Submissions, trả về danh sách các bài nộp
        public static List<Submission> GetSubmissions()
        {
            var submissions = new List<Submission>();
            using (var conn = new SQLiteConnection("Data Source=submissions.db;Version=3;"))
            {
                conn.Open();
                var command = new SQLiteCommand("SELECT * FROM Submissions", conn);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var submission = new Submission
                        {
                            Id = reader.GetInt32(0),
                            StudentId = reader.GetInt32(1),
                            FileName = reader.GetString(2),
                            Hash = reader.GetString(3),
                            SignatureR = reader.GetString(4),
                            SignatureS = reader.GetString(5),
                            SubmissionTime = reader.GetString(6),
                            Status = reader.GetString(7),
                            FilePath = reader.GetString(8)
                        };
                        if (reader.FieldCount > 9) submission.ClassName = reader.IsDBNull(9) ? null : reader.GetString(9);
                        if (reader.FieldCount > 10) submission.SubjectName = reader.IsDBNull(10) ? null : reader.GetString(10);
                        submissions.Add(submission);
                    }
                }
                conn.Close();
            }
            return submissions;
        }

        /// <summary>
        /// Cập nhật trạng thái bài nộp
        /// </summary>
        // Đổi trạng thái (Status) của một bài nộp theo submissionId
        public static void UpdateSubmissionStatus(int submissionId, string status)
        {
            using (var conn = new SQLiteConnection("Data Source=submissions.db;Version=3;"))
            {
                conn.Open();
                var command = new SQLiteCommand("UPDATE Submissions SET Status = @Status WHERE Id = @Id", conn);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Id", submissionId);
                command.ExecuteNonQuery();
                conn.Close();
            }
        }

        /// <summary>
        /// Lấy thông tin sinh viên
        /// </summary>
        // Truy vấn bảng Students để lấy thông tin sinh viên theo studentId
        public static Student GetStudent(int studentId)
        {
            using (var conn = new SQLiteConnection("Data Source=submissions.db;Version=3;"))
            {
                conn.Open();
                var command = new SQLiteCommand("SELECT * FROM Students WHERE Id = @Id", conn);
                command.Parameters.AddWithValue("@Id", studentId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Student
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            PublicKeyP = reader.GetString(2),
                            PublicKeyG = reader.GetString(3),
                            PublicKeyH = reader.GetString(4)
                        };
                    }
                }
                conn.Close();
            }
            return null;
        }

        /// <summary>
        /// Chỉ giải nén file (không xác thực)
        /// </summary>
        // Giải nén dữ liệu file gốc từ file đã ký, không kiểm tra chữ ký
        public static bool ExtractFileOnly(string filePath, string outputPathWithoutExt, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                var lines = File.ReadAllLines(filePath);
                if (lines.Length < 4)
                {
                    errorMessage = "Invalid file format";
                    return false;
                }

                byte[] data;
                try
                {
                    data = Convert.FromBase64String(lines[3]);
                }
                catch
                {
                    errorMessage = "Invalid file data format";
                    return false;
                }

                string extension = lines[0].TrimStart('.');
                string fullOutputPath = outputPathWithoutExt + "." + extension;
                File.WriteAllBytes(fullOutputPath, data);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Error extracting file: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Xóa bài nộp
        /// </summary>
        // Xóa một bài nộp khỏi bảng Submissions theo submissionId
        public static void DeleteSubmission(int submissionId)
        {
            using (var conn = new SQLiteConnection("Data Source=submissions.db;Version=3;"))
            {
                conn.Open();
                var command = new SQLiteCommand("DELETE FROM Submissions WHERE Id = @Id", conn);
                command.Parameters.AddWithValue("@Id", submissionId);
                command.ExecuteNonQuery();
                conn.Close();
            }
        }

        /// <summary>
        /// Cập nhật thông tin bài nộp
        /// </summary>
        // Cập nhật thông tin bài nộp và tên sinh viên trong cơ sở dữ liệu
        public static void UpdateSubmission(int submissionId, int studentId, string studentName, string className, string subjectName, string fileName, string submissionTime, string status, string filePath)
        {
            using (var conn = new SQLiteConnection("Data Source=submissions.db;Version=3;"))
            {
                conn.Open();

                try
                {
                    // Cập nhật bảng Submissions
                    var command = new SQLiteCommand(
                        "UPDATE Submissions SET StudentId = @StudentId, ClassName = @ClassName, SubjectName = @SubjectName, FileName = @FileName, SubmissionTime = @SubmissionTime, Status = @Status, FilePath = @FilePath WHERE Id = @Id",
                        conn);
                    command.Parameters.AddWithValue("@Id", submissionId);
                    command.Parameters.AddWithValue("@StudentId", studentId);
                    command.Parameters.AddWithValue("@ClassName", className ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SubjectName", subjectName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@FileName", fileName);
                    command.Parameters.AddWithValue("@SubmissionTime", submissionTime);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@FilePath", filePath);
                    command.ExecuteNonQuery();

                    // Cập nhật bảng Student
                    var commandSt = new SQLiteCommand(
                        "UPDATE Students SET Name = @StudentName WHERE Id = @StudentId",
                        conn);
                    commandSt.Parameters.AddWithValue("@StudentId", studentId);
                    commandSt.Parameters.AddWithValue("@StudentName", studentName ?? (object)DBNull.Value);
                    int rowsAffected = commandSt.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        throw new Exception("No rows updated in Student table. Check if StudentId exists.");
                    }
                }
                catch (SQLiteException ex)
                {
                    throw new Exception($"SQL Error updating submission: {ex.Message}");
                }
                finally
                {
                    conn.Close();
                }
            }
        }
    }
}