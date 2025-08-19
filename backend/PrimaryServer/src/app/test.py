#!/usr/bin/env python3
"""
Автоматический тестер для Auth API
Запуск: python test_auth_api.py
"""

import requests
import json
import sys
from typing import Dict, Optional, Tuple


class AuthAPITester:
    def __init__(self, base_url: str = "http://localhost:5000"):
        self.base_url = base_url
        self.auth_url = f"{base_url}/auth"
        self.admin_token = None
        self.manager_token = None
        self.test_results = []

    def log_test(self, test_name: str, success: bool, details: str = ""):
        """Логирование результатов тестов"""
        status = "✅ PASS" if success else "❌ FAIL"
        print(f"{status} {test_name}")
        if details:
            print(f"    {details}")
        self.test_results.append(
            {"test": test_name, "success": success, "details": details})

    def make_request(self, method: str, endpoint: str, data: dict = None,
                     token: str = None, expect_status: int = None) -> Tuple[Optional[dict], int]:
        """Выполнение HTTP запроса"""
        url = f"{self.auth_url}{endpoint}"
        headers = {"Content-Type": "application/json"}

        if token:
            headers["Authorization"] = f"Bearer {token}"

        try:
            if method == "GET":
                response = requests.get(url, headers=headers)
            elif method == "POST":
                response = requests.post(url, headers=headers, json=data)
            elif method == "PUT":
                response = requests.put(url, headers=headers, json=data)
            elif method == "DELETE":
                response = requests.delete(url, headers=headers)
            else:
                raise ValueError(f"Unsupported method: {method}")

            if expect_status and response.status_code != expect_status:
                return None, response.status_code

            try:
                return response.json(), response.status_code
            except json.JSONDecodeError:
                return None, response.status_code

        except requests.exceptions.ConnectionError:
            return None, 0

    def test_health_check(self):
        """Тест health check"""
        try:
            response = requests.get(f"{self.base_url}/health")
            if response.status_code == 200:
                self.log_test("Health Check", True)
                return True
        except:
            pass

        self.log_test("Health Check", False, "Server not responding")
        return False

    def test_registration(self):
        """Тест регистрации пользователей"""
        print("\n=== Testing User Registration ===")

        # Регистрация админа (первый пользователь)
        data, status = self.make_request("POST", "/auth/register", {
            "username": "admin",
            "password": "admin123"
        })

        success = status == 201 and data and data.get("role") == "admin"
        self.log_test("Admin Registration", success,
                      f"Status: {status}, Role: {data.get('role') if data else 'N/A'}")

        # Регистрация менеджера
        data, status = self.make_request("POST", "/auth/register", {
            "username": "manager",
            "password": "manager123"
        })

        success = status == 201 and data and data.get("role") == "manager"
        self.log_test("Manager Registration", success,
                      f"Status: {status}, Role: {data.get('role') if data else 'N/A'}")

        # Попытка дублированной регистрации
        data, status = self.make_request("POST", "/auth/register", {
            "username": "admin",
            "password": "newpass"
        })

        success = status == 409 and data and data.get(
            "code") == "USERNAME_EXISTS"
        self.log_test("Duplicate Registration Prevention", success,
                      f"Status: {status}, Code: {data.get('code') if data else 'N/A'}")

        # Валидация коротких данных
        data, status = self.make_request("POST", "/auth/register", {
            "username": "ab",
            "password": "123"
        })

        success = status == 400 and data and data.get(
            "code") == "VALIDATION_ERROR"
        self.log_test("Registration Validation", success,
                      f"Status: {status}, Code: {data.get('code') if data else 'N/A'}")

    def test_authentication(self):
        """Тест аутентификации"""
        print("\n=== Testing Authentication ===")

        # Успешная авторизация админа
        data, status = self.make_request("POST", "/auth/login", {
            "username": "admin",
            "password": "admin123"
        })

        if status == 200 and data and "access_token" in data:
            self.admin_token = data["access_token"]
            self.log_test("Admin Login", True, "Tokens received")
        else:
            self.log_test("Admin Login", False, f"Status: {status}")

        # Успешная авторизация менеджера
        data, status = self.make_request("POST", "/auth/login", {
            "username": "manager",
            "password": "manager123"
        })

        if status == 200 and data and "access_token" in data:
            self.manager_token = data["access_token"]
            self.log_test("Manager Login", True, "Tokens received")
        else:
            self.log_test("Manager Login", False, f"Status: {status}")

        # Неудачная авторизация
        data, status = self.make_request("POST", "/auth/login", {
            "username": "admin",
            "password": "wrongpass"
        })

        success = status == 401 and data and data.get(
            "code") == "INVALID_CREDENTIALS"
        self.log_test("Invalid Login Prevention", success,
                      f"Status: {status}, Code: {data.get('code') if data else 'N/A'}")

    def test_token_verification(self):
        """Тест проверки токенов"""
        print("\n=== Testing Token Verification ===")

        if not self.admin_token:
            self.log_test("Token Verification", False,
                          "No admin token available")
            return

        # Проверка валидного токена
        data, status = self.make_request(
            "GET", "/auth/verify", token=self.admin_token)

        success = (status == 200 and data and data.get("valid") and
                   data.get("user", {}).get("username") == "admin")
        self.log_test("Valid Token Verification", success,
                      f"Status: {status}, User: {data.get('user', {}).get('username') if data else 'N/A'}")

        # Проверка недействительного токена
        data, status = self.make_request(
            "GET", "/auth/verify", token="invalid_token")

        success = status == 401 and data and data.get(
            "code") == "TOKEN_REQUIRED"
        self.log_test("Invalid Token Rejection", success,
                      f"Status: {status}, Code: {data.get('code') if data else 'N/A'}")

    def test_authorization_levels(self):
        """Тест уровней авторизации"""
        print("\n=== Testing Authorization Levels ===")

        if not self.admin_token or not self.manager_token:
            self.log_test("Authorization Tests", False, "Missing tokens")
            return

        # Админский доступ для админа
        data, status = self.make_request(
            "GET", "/admin-only", token=self.admin_token)
        success = status == 200
        self.log_test("Admin Access for Admin", success, f"Status: {status}")

        # Админский доступ для менеджера (должен быть запрещен)
        data, status = self.make_request(
            "GET", "/admin-only", token=self.manager_token)
        success = status == 403 and data and data.get(
            "code") == "ADMIN_REQUIRED"
        self.log_test("Admin Access Denied for Manager", success,
                      f"Status: {status}, Code: {data.get('code') if data else 'N/A'}")

        # Менеджерский доступ для админа
        data, status = self.make_request(
            "GET", "/manager-or-admin", token=self.admin_token)
        success = status == 200
        self.log_test("Manager/Admin Access for Admin",
                      success, f"Status: {status}")

        # Менеджерский доступ для менеджера
        data, status = self.make_request(
            "GET", "/manager-or-admin", token=self.manager_token)
        success = status == 200
        self.log_test("Manager/Admin Access for Manager",
                      success, f"Status: {status}")

    def test_user_management(self):
        """Тест управления пользователями"""
        print("\n=== Testing User Management ===")

        if not self.admin_token:
            self.log_test("User Management Tests", False, "No admin token")
            return

        # Получение списка пользователей
        data, status = self.make_request(
            "GET", "/users", token=self.admin_token)
        success = status == 200 and data and "users" in data
        users_count = len(data.get("users", [])) if data else 0
        self.log_test("Get Users List", success,
                      f"Status: {status}, Users: {users_count}")

        # Создание нового пользователя
        data, status = self.make_request("POST", "/users", {
            "username": "testuser",
            "password": "test123",
            "role": "manager"
        }, token=self.admin_token)

        success = status == 201
        self.log_test("Create New User", success, f"Status: {status}")

        # Попытка менеджера получить список пользователей (должна быть запрещена)
        data, status = self.make_request(
            "GET", "/users", token=self.manager_token)
        success = status == 403 and data and data.get(
            "code") == "ADMIN_REQUIRED"
        self.log_test("Manager Access to Users Denied", success,
                      f"Status: {status}, Code: {data.get('code') if data else 'N/A'}")

    def test_profile_management(self):
        """Тест управления профилем"""
        print("\n=== Testing Profile Management ===")

        if not self.manager_token:
            self.log_test("Profile Management Tests",
                          False, "No manager token")
            return

        # Получение профиля
        data, status = self.make_request(
            "GET", "/profile", token=self.manager_token)
        success = status == 200 and data and "user" in data
        self.log_test("Get Profile", success, f"Status: {status}")

        # Обновление пароля
        data, status = self.make_request("PUT", "/profile", {
            "password": "newpassword123"
        }, token=self.manager_token)

        success = status == 200
        self.log_test("Update Password", success, f"Status: {status}")

    def run_all_tests(self):
        """Запуск всех тестов"""
        print("🚀 Starting Auth API Tests...")
        print("=" * 50)

        # Проверка подключения к серверу
        if not self.test_health_check():
            print("❌ Cannot connect to server. Make sure it's running on", self.base_url)
            return False

        # Основные тесты
        self.test_registration()
        self.test_authentication()
        self.test_token_verification()
        self.test_authorization_levels()
        self.test_user_management()
        self.test_profile_management()

        # Итоговая статистика
        print("\n" + "=" * 50)
        print("📊 TEST RESULTS SUMMARY")
        print("=" * 50)

        passed = sum(1 for result in self.test_results if result["success"])
        total = len(self.test_results)

        print(f"Tests passed: {passed}/{total}")
        print(f"Success rate: {passed/total*100:.1f}%")

        if passed == total:
            print("🎉 All tests passed!")
        else:
            print("⚠️  Some tests failed. Check the details above.")

        return passed == total


def main():
    """Главная функция"""
    import argparse

    parser = argparse.ArgumentParser(description="Test Auth API")
    parser.add_argument("--url", default="http://localhost:5000",
                        help="Base URL of the API server")
    args = parser.parse_args()

    tester = AuthAPITester(args.url)

    try:
        success = tester.run_all_tests()
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\n⚠️ Tests interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
