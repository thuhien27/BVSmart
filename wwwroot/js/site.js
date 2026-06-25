function sendChat() {
    var userInput = $("#chatInput").val();
    if (!userInput || userInput.trim() === "") return;

    $("#chatBox").append("<div class='user-msg'><b>Bạn:</b> " + userInput + "</div>");

    // Tạo hiệu ứng chờ
    var tempId = "loading-" + Date.now();
    $("#chatBox").append("<div id='" + tempId + "'><i>Đang phân tích triệu chứng...</i></div>");

    $("#chatInput").val("");

    $.ajax({
        // ĐỊA CHỈ PHẢI KHỚP VỚI [Route("api/[controller]")] và [HttpPost("predict")]
        url: '/api/Chatbot/predict',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ Symptoms: userInput }), // 'Symptoms' phải viết hoa chữ S
        success: function (data) {
            $("#" + tempId).remove(); // Xóa dòng đang phân tích

            // Lấy kết quả từ data.result (theo chuẩn return Ok(new { result }))
            if (data && data.result) {
                $("#chatBox").append("<div style='color:blue; margin-top:10px;'><b>AI:</b> " + data.result + "</div>");
            }

            var cb = document.getElementById("chatBox");
            if (cb) cb.scrollTop = cb.scrollHeight;
        },
        error: function (xhr) {
            $("#" + tempId).remove();
            console.error("Lỗi chi tiết:", xhr.responseText);
            // Nếu vẫn lỗi 404, hãy thử đổi url thành: '/api/Ai/predict'
            alert("Lỗi kết nối: " + xhr.status);
        }
    });
}