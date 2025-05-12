$(document).ready(function () {
    // Sidebar menu functionality
    $('.submenu > a').on('click', function (e) {
        e.preventDefault();
        $(this).parent().toggleClass('active');
        $(this).parent().find('ul').slideToggle(300);
    });

    // Mobile sidebar toggle
    $('#mobile_btn').on('click', function () {
        $(this).toggleClass('active');
        $('#sidebar').toggleClass('collapsed');
        $('.page-wrapper').toggleClass('slide-content');
    });

    // Initialize DataTables where present
    if ($.fn.DataTable) {
        $('.datatable').DataTable({
            "dom": '<"top"fl>rt<"bottom"ip><"clear">',
            "language": {
                search: "_INPUT_",
                searchPlaceholder: "Search..."
            },
            responsive: true
        });
    }

    // Animations for page content
    $('.card').addClass('fade-in');
}); 