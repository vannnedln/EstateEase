$(document).ready(function() {
    // Toggle Sidebar
    $("#toggle_btn").on("click", function() {
        if($('body').hasClass('mini-sidebar')) {
            $('body').removeClass('mini-sidebar');
            $('.sidebar').removeClass('active');
        } else {
            $('body').addClass('mini-sidebar');
            $('.sidebar').addClass('active');
        }
    });

    // Mobile Menu Toggle
    $("#mobile_btn").on("click", function() {
        $('.sidebar').toggleClass('active');
    });

    // Submenu toggle
    $('.submenu > a').on('click', function(e) {
        e.preventDefault();
        $(this).next('ul').slideToggle();
    });
});