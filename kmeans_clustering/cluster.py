import numpy as np
import MySQLdb
import pymysql
import random


def cat_utility(ds, clustering, m):
    n = len(ds)
    d = len(ds[0])

    cluster_cts = [0] * m
    for ni in range(n):
        k = clustering[ni]
        cluster_cts[k] += 1

    for i in range(m):
        if cluster_cts[i] == 0:
            return 0.0

    unique_vals = [0] * d
    for i in range(d):
        maxi = 0
        for ni in range(n):
            if ds[ni][i] > maxi:
                maxi = ds[ni][i]
        unique_vals[i] = maxi + 1

    att_cts = []
    for i in range(d):
        cts = [0] * unique_vals[i]
        for ni in range(n):
            v = ds[ni][i]
            cts[v] += 1
        att_cts.append(cts)

    k_cts = []
    for k in range(m):
        a_cts = []
        for i in range(d):
            cts = [0] * unique_vals[i]
            for ni in range(n):
                if clustering[ni] != k:
                    continue
                v = ds[ni][i]
                cts[v] += 1
            a_cts.append(cts)
        k_cts.append(a_cts)

    un_sum_sq = 0.0
    for i in range(d):
        for j in range(len(att_cts[i])):
            un_sum_sq += (1.0 * att_cts[i][j] / n) * (1.0 * att_cts[i][j] / n)

    cond_sum_sq = [0.0] * m
    for k in range(m):
        sum = 0.0
        for i in range(d):
            for j in range(len(att_cts[i])):
                if cluster_cts[k] == 0: print "FATAL LOGIC ERROR"
                sum += (1.0 * k_cts[k][i][j] / cluster_cts[k]) * (1.0 * k_cts[k][i][j] / cluster_cts[k])
        cond_sum_sq[k] = sum

    prob_c = [0.0] * m
    for k in range(m):
        prob_c[k] = (1.0 * cluster_cts[k]) / n

    left = 1.0 / m
    right = 0.0
    for k in range(m):
        right += prob_c[k] * (cond_sum_sq[k] - un_sum_sq)
    cu = left * right
    return cu


def cluster(ds, m):
    n = len(ds)
    working_set = [0] * m
    for k in range(m):
        working_set[k] = list(ds[k])

    clustering = list(range(m))

    for i in range(m, n):
        item_to_cluster = ds[i]
        working_set.append(item_to_cluster)

        proposed_clusterings = []
        for k in range(m):
            copy_of_clustering = list(clustering)
            copy_of_clustering.append(k)
            proposed_clusterings.append(copy_of_clustering)

        proposed_cus = [0.0] * m
        for k in range(m):
            proposed_cus[k] = cat_utility(working_set, proposed_clusterings[k], m)
        best_proposed = np.argmax(proposed_cus)

        clustering.append(best_proposed)

    return clustering


def main():
    db = pymysql.connect(host='localhost', user='root', passwd='djansr8041', db='disco')
    # Create a Cursor object to execute queries.
    cur = db.cursor()
    while True:
        while True:
            category = raw_input("Choose category for clustering: (genre, style): ")
            print category
            if category in ["genre", "style"]:
                break
        while True:
            m = raw_input("Choose number of clusters(must be positive integer [1, 10]): ")
            m = int(m)
            if m > 0 and m <= 10:
                break
        if category == "genre":
            cur.execute("select count(*) from Genre")
            res = cur.fetchone()
            d = int(res[0])

            dict = {}
            # Select data from table using SQL query.
            cur.execute("SELECT AlbumId, GenreId FROM AlbumGenre")

            for row in cur.fetchall():
                if row[0] not in dict:
                    dict[row[0]] = [int(x) for x in np.zeros(d)]
                dict[row[0]][row[1] - 1] = 1
            enc_data = []
            keys = dict.keys()
            random.shuffle(keys)
            for key in keys:
                enc_data.append(dict[key])
            clustering = cluster(enc_data, m)
            cu = cat_utility(enc_data, clustering, m)
            print "Category utility of clustering = %0.4f \n" % cu
            dict_res = {}
            for k in range(m):
                for i, key in enumerate(keys):
                    if clustering[i] == k:
                        dict_res[key] = k
                        # print "cluster number ", key, " => ", k
            for key in dict_res.keys():
                print "cluster number ", key, " => ", dict_res[key]
        elif category == "style":
            pass
        elif category == "year":
            pass


if __name__ == "__main__":
    main()
