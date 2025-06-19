$(document).ready(function() {
    var system_user_list = $('#system_user_list').DataTable({
        "responsive": true, 
        "paging": true,
        "searching": true,
        "ordering": true,
        "processing": true,
        "serverSide": true,
        //"dom": 'Bfrtip',
        "ajax": {
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            "url": '/system_user/getList',
            "type": "POST"
        }
    });

    $('#system_user_list').on('click', '[data-rowid]', function(){
        var row_id = $(this).attr('data-rowid');

        var status = $(this).html();
        //alert(row_id);

        $.ajax({
            type: "POST",
            url: "/system_user/status_toggle/",
            headers: {
                'X-CSRF-TOKEN': $('meta[name="csrf-token"]').attr('content')
            },
            dataType : "json",
            cache : false,
            data:{
                "row_id": row_id,
                "status": status
            },
            beforeSend: function(){
                toastr["info"]("Updating Status");
                loaderShow('main_loader');

            },success: function(response){
                loaderHide('main_loader');

                if(response.status == 'success'){

                    console.log(response.statusC);
                
                    if(response.statusC == 'Active'){

                        $('[data-rowid='+row_id+']').removeClass('badge-danger').addClass('badge-secondary');
                        $('[data-rowid='+row_id+']').html(response.statusC);
                    }else if(response.statusC == 'In Active'){
                        $('[data-rowid='+row_id+']').removeClass('badge-secondary').addClass('badge-danger');
                        $('[data-rowid='+row_id+']').html(response.statusC);
                    }

                }

            },error : function(){
                loaderHide('main_loader');
                toastr["error"]("Some Error Occured");
            }
        });

    });
});