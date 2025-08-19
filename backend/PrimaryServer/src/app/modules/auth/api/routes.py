# from http import HTTPStatus
# from flask import request, jsonify
# from .. import authBlueprint

# @authBlueprint.route("/login", methods=["POST"])
# def login():
#     data = request.get_json()
#     username = data.get("username")
#     password = data.get("password")

#     user = get_user_by_username(username)
#     if not user:
#         return jsonify({"error": "Invalid credentials"}), HTTPStatus.UNAUTHORIZED

#     if not bcrypt.checkpw(password.encode("utf-8"), user["password"].encode("utf-8")):
#         return jsonify({"error": "Invalid credentials"}), 401

#     token = create_jwt(user["id"])
#     return jsonify({"token": token})