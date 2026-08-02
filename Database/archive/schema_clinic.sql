-- ==========================================
-- CLINIC & HOSPITAL MANAGEMENT SCHEMA ADDITIONS
-- ==========================================

-- 1. Add roles for Doctor and Pharmacist if they don't exist
INSERT INTO roles (id, name) VALUES 
(3, 'Doctor'),
(4, 'Pharmacist')
ON CONFLICT (id) DO NOTHING;

-- 2. Create Patients table
CREATE TABLE IF NOT EXISTS patients (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    gender VARCHAR(50),
    date_of_birth DATE,
    phone VARCHAR(50) UNIQUE,
    address TEXT,
    medical_history TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 3. Create Appointments table
CREATE TABLE IF NOT EXISTS appointments (
    id SERIAL PRIMARY KEY,
    patient_id INT REFERENCES patients(id) ON DELETE CASCADE,
    doctor_id INT REFERENCES users(id) ON DELETE SET NULL,
    appointment_date TIMESTAMP NOT NULL,
    reason TEXT,
    status VARCHAR(50) DEFAULT 'Scheduled', -- 'Scheduled', 'Completed', 'Cancelled'
    notes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 4. Alter Prescriptions table to make user_id nullable and add patient_id reference
ALTER TABLE prescriptions ADD COLUMN IF NOT EXISTS patient_id INT REFERENCES patients(id) ON DELETE SET NULL;
ALTER TABLE prescriptions ALTER COLUMN user_id DROP NOT NULL;

-- 5. Seed some sample patients
INSERT INTO patients (id, name, gender, date_of_birth, phone, address, medical_history) VALUES
(1, 'Nguyễn Văn Hùng', 'Nam', '1975-06-15', '0905111222', '123 Đường Lê Lợi, Đà Nẵng', 'Cao huyết áp, đau khớp gối khi thời tiết thay đổi'),
(2, 'Trần Thị Mai', 'Nữ', '1988-11-23', '0905333444', '456 Đường Hùng Vương, Quảng Nam', 'Suy nhược cơ thể, mất ngủ kéo dài'),
(3, 'Lê Hoàng Nam', 'Nam', '1995-02-10', '0905555666', '789 Đường Điện Biên Phủ, Đà Nẵng', 'Dị ứng phấn hoa, viêm xoang mãn tính')
ON CONFLICT (id) DO NOTHING;

-- 6. Seed some default Doctor and Pharmacist accounts
INSERT INTO users (id, username, email, password_hash, phone, role_id, is_active) VALUES
(10, 'doctor', 'doctor@tmpms.com', 'doctor123', '0905999999', 3, TRUE),
(11, 'pharmacist', 'pharmacist@tmpms.com', 'pharmacist123', '0905888888', 4, TRUE)
ON CONFLICT (id) DO NOTHING;

-- 7. Seed some sample appointments
INSERT INTO appointments (id, patient_id, doctor_id, appointment_date, reason, status, notes) VALUES
(1, 1, 10, NOW() + INTERVAL '1 day', 'Tái khám định kỳ huyết áp và xương khớp', 'Scheduled', 'Bệnh nhân cần đo lại huyết áp lúc đói'),
(2, 2, 10, NOW() + INTERVAL '2 days', 'Khám chứng mất ngủ và chóng mặt', 'Scheduled', 'Theo dõi giấc ngủ trong 1 tuần qua'),
(3, 3, 10, NOW() - INTERVAL '3 days', 'Khám viêm xoang và nghẹt mũi', 'Completed', 'Đã kê đơn thuốc xông và thảo dược giải độc')
ON CONFLICT (id) DO NOTHING;

-- Reset sequences
SELECT setval('patients_id_seq', COALESCE((SELECT MAX(id)+1 FROM patients), 1), false);
SELECT setval('appointments_id_seq', COALESCE((SELECT MAX(id)+1 FROM appointments), 1), false);
SELECT setval('users_id_seq', COALESCE((SELECT MAX(id)+1 FROM users), 1), false);
